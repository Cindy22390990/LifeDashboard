export type V4TransactionType =
  | 'income'
  | 'expense'
  | 'transfer'
  | 'unknown';

export type V4AccountType =
  | 'cashflow'
  | 'saving'
  | 'invest'
  | 'unknown';

export interface DateOnlyViewModel {
  isoDate: string;
  year: number;
  month: number;
  day: number;
}

export interface V4TransactionItemViewModel {
  id: string;
  title: string;
  amount: number;
  transactionType: V4TransactionType;
  transactionDate: DateOnlyViewModel;
  accountId: string | null;
  accountName: string | null;
  categoryId: string | null;
  categoryName: string | null;
}

export interface V4HomeViewModel {
  businessDate: DateOnlyViewModel;
  dailySpendingLimit: number;
  dailySpent: number;
  dailyRemaining: number;
  monthlyProgress: {
    saved: number;
    invested: number;
  };
  todayTransactions: V4TransactionItemViewModel[];
}

export interface V4TransactionListViewModel {
  totalCount: number;
  page: number;
  pageSize: number;
  totalIncome: number;
  totalExpense: number;
  items: V4TransactionItemViewModel[];
}

export interface V4MeAssetsViewModel {
  totalAssets: number;
  accounts: Array<{
    id: string;
    name: string;
    accountType: V4AccountType;
    balance: number;
  }>;
}

export interface V4AccountReadItemViewModel {
  id: string;
  name: string;
  accountType: V4AccountType;
  balance: number;
}

export interface V4AccountsReadViewModel {
  totalAssets: number;
  items: V4AccountReadItemViewModel[];
}

export interface V4CategoryReadViewModel {
  id: string;
  name: string;
  icon: string;
  bucketType: 1 | 2 | 3 | 4 | 5;
  bucketTypeName: string;
}
