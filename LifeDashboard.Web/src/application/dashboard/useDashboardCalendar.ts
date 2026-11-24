import { useCallback, useEffect, useRef, useState } from 'react';
import type { IDashboardDataSource } from './ports/IDashboardDataSource';
import type {
  CalendarDayDetailViewModel,
  CalendarMonthViewModel,
} from './models/DashboardViewModel';

interface CalendarMonthKey {
  year: number;
  month: number;
}

interface CalendarRequestState<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
}

interface CalendarSelection {
  selectedDate: string;
  viewedMonth: CalendarMonthKey;
}

function getMonthFromDate(date: string): CalendarMonthKey {
  const [year, month] = date.split('-').map(Number);
  return { year, month };
}

function formatDateKey(year: number, month: number, day: number): string {
  return `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
}

function shiftMonth(
  current: CalendarMonthKey,
  offset: number,
): CalendarMonthKey {
  const absoluteMonth = current.year * 12 + current.month - 1 + offset;
  const year = Math.floor(absoluteMonth / 12);

  return {
    year,
    month: absoluteMonth - year * 12 + 1,
  };
}

export function useDashboardCalendar(
  dataSource: IDashboardDataSource,
  initialDate: string | null,
) {
  const [selection, setSelection] = useState<CalendarSelection | null>(() =>
    initialDate
      ? { selectedDate: initialDate, viewedMonth: getMonthFromDate(initialDate) }
      : null);
  const [dayRetry, setDayRetry] = useState(0);
  const [monthRetry, setMonthRetry] = useState(0);
  const selectionRef = useRef(selection);
  const dayControllerRef = useRef<AbortController | null>(null);
  const monthControllerRef = useRef<AbortController | null>(null);
  const [dayDetail, setDayDetail] = useState<CalendarRequestState<CalendarDayDetailViewModel>>({
    data: null,
    loading: false,
    error: null,
  });
  const [monthSummary, setMonthSummary] = useState<CalendarRequestState<CalendarMonthViewModel>>({
    data: null,
    loading: false,
    error: null,
  });
  const dayRequestId = useRef(0);
  const monthRequestId = useRef(0);

  useEffect(() => {
    if (!initialDate || selectionRef.current) return;

    const initialSelection = {
      selectedDate: initialDate,
      viewedMonth: getMonthFromDate(initialDate),
    };
    selectionRef.current = initialSelection;
    setSelection(initialSelection);
  }, [initialDate]);

  useEffect(() => {
    if (!selection) {
      setDayDetail({ data: null, loading: false, error: null });
      return;
    }

    const controller = new AbortController();
    dayControllerRef.current = controller;
    const requestId = ++dayRequestId.current;
    const { selectedDate } = selection;
    setDayDetail({ data: null, loading: true, error: null });

    void dataSource.getCalendarDayDetail(selectedDate, {
      signal: controller.signal,
    }).then((data) => {
      if (controller.signal.aborted || requestId !== dayRequestId.current) return;
      setDayDetail({ data, loading: false, error: null });
    }).catch((error: unknown) => {
      if (controller.signal.aborted || requestId !== dayRequestId.current) return;
      setDayDetail({
        data: null,
        loading: false,
        error: error instanceof Error ? error.message : String(error),
      });
    });

    return () => {
      controller.abort();
      if (dayControllerRef.current === controller) {
        dayControllerRef.current = null;
      }
    };
  }, [dataSource, selection?.selectedDate, dayRetry]);

  useEffect(() => {
    if (!selection) {
      setMonthSummary({ data: null, loading: false, error: null });
      return;
    }

    const controller = new AbortController();
    monthControllerRef.current = controller;
    const requestId = ++monthRequestId.current;
    const { year, month } = selection.viewedMonth;
    setMonthSummary({ data: null, loading: true, error: null });

    void dataSource.getCalendarMonthSummary(year, month, {
      signal: controller.signal,
    }).then((data) => {
      if (controller.signal.aborted || requestId !== monthRequestId.current) return;
      setMonthSummary({ data, loading: false, error: null });
    }).catch((error: unknown) => {
      if (controller.signal.aborted || requestId !== monthRequestId.current) return;
      setMonthSummary({
        data: null,
        loading: false,
        error: error instanceof Error ? error.message : String(error),
      });
    });

    return () => {
      controller.abort();
      if (monthControllerRef.current === controller) {
        monthControllerRef.current = null;
      }
    };
  }, [dataSource, selection?.viewedMonth.year, selection?.viewedMonth.month, monthRetry]);

  const selectDate = useCallback((date: string) => {
    const viewedMonth = getMonthFromDate(date);
    const currentSelection = selectionRef.current;
    dayRequestId.current += 1;
    dayControllerRef.current?.abort();
    if (
      currentSelection &&
      (currentSelection.viewedMonth.year !== viewedMonth.year ||
        currentSelection.viewedMonth.month !== viewedMonth.month)
    ) {
      monthRequestId.current += 1;
      monthControllerRef.current?.abort();
    }

    const nextSelection = {
      selectedDate: date,
      viewedMonth,
    };
    selectionRef.current = nextSelection;
    setSelection(nextSelection);
  }, []);

  const navigateMonth = useCallback((offset: number) => {
    const currentSelection = selectionRef.current;
    if (!currentSelection) return;

    const viewedMonth = shiftMonth(currentSelection.viewedMonth, offset);
    if (viewedMonth.year < 1 || viewedMonth.year > 9999) return;

    dayRequestId.current += 1;
    monthRequestId.current += 1;
    dayControllerRef.current?.abort();
    monthControllerRef.current?.abort();

    const nextSelection = {
      selectedDate: formatDateKey(viewedMonth.year, viewedMonth.month, 1),
      viewedMonth,
    };
    selectionRef.current = nextSelection;
    setSelection(nextSelection);
  }, []);

  const retryDay = useCallback(() => setDayRetry((retry) => retry + 1), []);
  const retryMonth = useCallback(() => setMonthRetry((retry) => retry + 1), []);

  return {
    selectedDate: selection?.selectedDate ?? null,
    viewedMonth: selection?.viewedMonth ?? null,
    dayDetail,
    monthSummary,
    selectDate,
    navigateMonth,
    retryDay,
    retryMonth,
  };
}
