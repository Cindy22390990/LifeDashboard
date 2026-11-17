import type { IAccount } from './Account';
import type { IGoal } from './Goal';
import type { RadarStateDto } from './budgetRadar';
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
  budget: RadarStateDto;
  accounts: IAccount[];
  goals: IGoal[];
  totalAssets: number;
  monthlyIncome: number;
  monthlyExpense: number;
  recentTransactions: DashboardTransactionReadModel[];
}