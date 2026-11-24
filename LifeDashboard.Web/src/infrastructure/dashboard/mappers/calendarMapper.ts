import type {
  CalendarDayDetailViewModel,
  CalendarDaySummaryViewModel,
  CalendarMonthViewModel,
} from '../../../application/dashboard/models/DashboardViewModel';
import type { DashboardCalendarDayResponse } from '../api/DashboardCalendarDayResponse';
import type { DashboardCalendarMonthResponse } from '../api/DashboardCalendarMonthResponse';
import { mapRecentTransaction } from './recentTransactionMapper';

function getActivityMarker(
  totalIncome: number,
  totalExpense: number,
  hasTransfer: boolean,
): CalendarDaySummaryViewModel['marker'] {
  if (totalIncome > 0) return 'income';
  if (totalExpense > 0) return 'expense';
  if (hasTransfer) return 'transfer';
  return undefined;
}

export function mapDashboardCalendarDay(
  response: DashboardCalendarDayResponse,
): CalendarDayDetailViewModel {
  const transactions = response.transactions.map(mapRecentTransaction);

  return {
    date: response.date,
    totalIncome: response.totalIncome,
    totalExpense: response.totalExpense,
    transactions,
    status: transactions.length > 0 ? 'ready' : 'empty',
  };
}

export function mapDashboardCalendarMonth(
  response: DashboardCalendarMonthResponse,
): CalendarMonthViewModel {
  const daysByDate = Object.fromEntries(
    response.days.map((day) => [
      day.date,
      {
        ...day,
        marker: getActivityMarker(
          day.totalIncome,
          day.totalExpense,
          day.hasTransfer,
        ),
      },
    ]),
  );

  return {
    year: response.year,
    month: response.month,
    daysByDate,
  };
}
