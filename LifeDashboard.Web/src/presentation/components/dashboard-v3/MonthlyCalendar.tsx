import { useTranslation } from 'react-i18next';
import { formatDate } from '../../../i18n/formatters';
import type { CalendarMonthViewModel } from '../../../application/dashboard/models/DashboardViewModel';
import './MonthlyCalendar.css';

export type CalendarViewMode = 'week' | 'month';

interface CalendarDay {
  date: string;
  isCurrentMonth: boolean;
}

interface MonthlyCalendarProps {
  viewModel: CalendarMonthViewModel;
  locale: string;
  isMobile: boolean;
  selectedDate: string;
  viewMode: CalendarViewMode;
  onExpandMonth: () => void;
  onNavigateMonth: (offset: number) => void;
  onSelectDate: (date: string) => void;
}

function toDateKey(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function getMonthDays(year: number, month: number): CalendarDay[] {
  const monthIndex = month - 1;
  const firstDay = new Date(year, monthIndex, 1);
  const mondayOffset = (firstDay.getDay() + 6) % 7;
  const gridStart = new Date(year, monthIndex, 1 - mondayOffset);

  return Array.from({ length: 42 }, (_, index) => {
    const date = new Date(gridStart);
    date.setDate(gridStart.getDate() + index);
    return {
      date: toDateKey(date),
      isCurrentMonth: date.getMonth() === monthIndex,
    };
  });
}

function getWeekDays(selectedDate: string): CalendarDay[] {
  const [year, month, day] = selectedDate.split('-').map(Number);
  const selected = new Date(year, month - 1, day);
  const mondayOffset = (selected.getDay() + 6) % 7;
  const weekStart = new Date(selected);
  weekStart.setDate(selected.getDate() - mondayOffset);

  return Array.from({ length: 7 }, (_, index) => {
    const date = new Date(weekStart);
    date.setDate(weekStart.getDate() + index);
    return {
      date: toDateKey(date),
      isCurrentMonth: date.getMonth() === selected.getMonth(),
    };
  });
}

export default function MonthlyCalendar({
  viewModel,
  locale,
  isMobile,
  selectedDate,
  viewMode,
  onExpandMonth,
  onNavigateMonth,
  onSelectDate,
}: MonthlyCalendarProps) {
  const { t } = useTranslation();
  const monthDate = `${viewModel.year}-${String(viewModel.month).padStart(2, '0')}-01`;
  const monthLabel = formatDate(monthDate, locale, {
    month: 'long',
    year: 'numeric',
  });
  const days = viewMode === 'week'
    ? getWeekDays(selectedDate)
    : getMonthDays(viewModel.year, viewModel.month);
  const weekdays = Array.from({ length: 7 }, (_, index) => {
    const date = new Date(Date.UTC(2024, 0, index + 1));
    return formatDate(date, locale, { weekday: 'short', timeZone: 'UTC' });
  });

  return (
    <div className={`v3-monthly-calendar v3-monthly-calendar--${viewMode}`}>
      <div className="v3-monthly-calendar__heading">
        <div className="v3-monthly-calendar__month-navigation">
          <button
            aria-label={t('calendar.previousMonth')}
            onClick={() => onNavigateMonth(-1)}
            type="button"
          >
            ‹
          </button>
          <strong>{monthLabel}</strong>
          <button
            aria-label={t('calendar.nextMonth')}
            onClick={() => onNavigateMonth(1)}
            type="button"
          >
            ›
          </button>
        </div>
        {isMobile && (
          <button
            aria-expanded={viewMode === 'month'}
            className="v3-monthly-calendar__expand"
            onClick={onExpandMonth}
            type="button"
          >
            {viewMode === 'month'
              ? t('calendar.collapseMonth')
              : t('calendar.expandMonth')}
            <span aria-hidden="true">{viewMode === 'month' ? '⌃' : '⌄'}</span>
          </button>
        )}
      </div>
      <div className="v3-monthly-calendar__weekdays" aria-hidden="true">
        {weekdays.map((weekday) => <span key={weekday}>{weekday}</span>)}
      </div>
      <div className={`v3-monthly-calendar__days v3-monthly-calendar__days--${viewMode}`}>
        {days.map(({ date, isCurrentMonth }) => {
          const marker = viewModel.daysByDate[date]?.marker;
          const dayNumber = Number(date.slice(-2));
          const markerIcon = marker === 'income' ? '↑' : marker === 'expense' ? '↓' : '↔';
          const activity = marker
            ? t(`calendar.markers.${marker}`)
            : t('calendar.markers.none');

          return (
            <button
              aria-label={t('calendar.dayAriaLabel', {
                date: formatDate(date, locale, { dateStyle: 'full' }),
                activity,
              })}
              aria-pressed={selectedDate === date}
              className={[
                'v3-calendar-day',
                selectedDate === date ? 'is-selected' : '',
                !isCurrentMonth ? 'is-outside-month' : '',
              ].filter(Boolean).join(' ')}
              key={date}
              onClick={() => onSelectDate(date)}
              type="button"
            >
              <span className="v3-calendar-day__number">{dayNumber}</span>
              <span
                aria-hidden="true"
                className={`v3-calendar-day__marker${marker ? ` v3-calendar-day__marker--${marker}` : ''}`}
              >
                {marker ? markerIcon : ''}
              </span>
            </button>
          );
        })}
      </div>
    </div>
  );
}
