export interface TransactionItemDto {
  id: string;
  note: string | null;
  amount: number;
  transactionType: 'Expense' | 'Income' | 'Transfer';
  transactionDate: string;
  accountId?: string;
  accountName?: string;
  categoryId?: string;
  categoryName?: string;
}

export interface TransactionListFilter {
  year?: number;
  month?: number;
  date?: string;
  keyword?: string;
  type?: 'Expense' | 'Income' | 'Transfer';
  page?: number;
  pageSize?: number;
}

export interface TransactionListResponseDto {
  totalCount: number;
  page: number;
  pageSize: number;
  totalIncome: number;
  totalExpense: number;
  items: TransactionItemDto[];
}

export interface QuickCreateTransactionRequest {
  amount: number;
  note: string | null;
  transactionType: number;
}
