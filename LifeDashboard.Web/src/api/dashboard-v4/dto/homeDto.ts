export interface MonthlyProgressDto {
  saved: number;
  invested: number;
}

export interface HomeTransactionItemDto {
  id: string;
  note: string | null;
  amount: number;
  transactionType: 'Expense' | 'Income' | 'Transfer';
  transactionDate: string;
}

export interface DashboardV4HomeDto {
  businessDate: string;
  dailySpendingLimit: number;
  dailySpent: number;
  dailyRemaining: number;
  monthlyProgress: MonthlyProgressDto;
  todayTransactions: HomeTransactionItemDto[];
}
