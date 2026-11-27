import type { TransactionType } from './Transaction';

export interface DashboardTransactionReadModel {
  id: string;
  accountId: string;
  accountName: string | null;
  toAccountId: string | null;
  toAccountName: string | null;
  amount: number;
  type: TransactionType;
  categoryId: string | null;
  categoryEntityId: string | null;
  description: string;
  transactionDate: string;
}

export interface DashboardSummaryDto {
  totalAssets: number;
  monthlyIncome: number;
  monthlyExpense: number;
}