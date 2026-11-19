import { useTranslation } from 'react-i18next';
import { formatDate } from '../../../i18n/formatters';
import { mockDashboardData } from './mockDashboardData';
import './MonthlyCalendar.css';

export type CalendarViewMode = 'week' | 'month';

interface CalendarDay {
  date: string;
  isCurrentMonth: boolean;
}

interface MonthlyCalendarProps {
  currentDate: string;
  locale: string;
  isMobile: boolean;
  selectedDate: string;
  viewMode: CalendarViewMode;
  onExpandMonth: () => void;
  onSelectDate: (date: string) => void;
}

function toDateKey(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function getMonthDays(currentDate: string): CalendarDay[] {
  const referenceDate = new Date(`${currentDate}T00:00:00`);
  const year = referenceDate.getFullYear();
  const month = referenceDate.getMonth();
  const firstDay = new Date(year, month, 1);
  const mondayOffset = (firstDay.getDay() + 6) % 7;
  const gridStart = new Date(year, month, 1 - mondayOffset);

  return Array.from({ length: 42 }, (_, index) => {
    const date = new Date(gridStart);
    date.setDate(gridStart.getDate() + index);
    return {
      date: toDateKey(date),
      isCurrentMonth: date.getMonth() === month,
    };
  });
}

function getWeekDays(selectedDate: string): CalendarDay[] {
  const selected = new Date(`${selectedDate}T00:00:00`);
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

function getMarker(date: string): string | undefined {
  return Object.entries(mockDashboardData.calendarMarkers)
    .find(([markerDate]) => markerDate === date)?.[1];
}

export default function MonthlyCalendar({
  currentDate,
  locale,
  isMobile,
  selectedDate,
  viewMode,
  onExpandMonth,
  onSelectDate,
}: MonthlyCalendarProps) {
  const { t } = useTranslation();
  const monthLabel = formatDate(currentDate, locale, {
    month: 'long',
    year: 'numeric',
  });
  const days = viewMode === 'week'
    ? getWeekDays(selectedDate)
    : getMonthDays(currentDate);
  const weekdays = Array.from({ length: 7 }, (_, index) => {
    const date = new Date(Date.UTC(2024, 0, index + 1));
    return formatDate(date, locale, { weekday: 'short', timeZone: 'UTC' });
  });

  return (
    <div className={`v3-monthly-calendar v3-monthly-calendar--${viewMode}`}>
      <div className="v3-monthly-calendar__heading">
        <strong>{monthLabel}</strong>
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
          const marker = getMarker(date);
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
