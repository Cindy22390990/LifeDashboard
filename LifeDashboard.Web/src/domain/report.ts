export interface MonthlyReport {
  year: number;
  month: number;
  totalIncome: number;
  totalExpense: number;
  balance: number;
  categories: CategoryBreakdown[];
}

export interface CategoryBreakdown {
  categoryId: string | null;
  categoryName: string;
  bucketType: string | number | null;
  income: number;
  expense: number;
  transactionCount: number;
}
