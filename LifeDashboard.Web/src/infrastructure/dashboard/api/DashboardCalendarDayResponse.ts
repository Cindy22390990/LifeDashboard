export type CalendarTransactionType = 'Expense' | 'Income' | 'Transfer';

export interface DayTransactionItemDto {
  id: string;
  note?: string | null;
  categoryName: string;
  accountName: string;
  amount: number;
  type: CalendarTransactionType;
  transactionDate: string;
}

export interface DashboardCalendarDayResponse {
  date: string;
  totalIncome: number;
  totalExpense: number;
  transactions: DayTransactionItemDto[];
}
