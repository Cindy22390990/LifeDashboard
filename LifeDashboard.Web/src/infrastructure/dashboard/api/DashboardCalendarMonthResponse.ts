export interface CalendarDaySummaryDto {
  date: string;
  totalIncome: number;
  totalExpense: number;
  hasTransfer: boolean;
}

export interface DashboardCalendarMonthResponse {
  year: number;
  month: number;
  days: CalendarDaySummaryDto[];
}
