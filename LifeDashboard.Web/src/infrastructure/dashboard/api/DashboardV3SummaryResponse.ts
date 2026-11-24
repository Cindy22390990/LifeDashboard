import type { AccountSummaryDto } from './AccountSummaryDto';
import type { RecentTransactionDto } from './RecentTransactionDto';

export interface DashboardSummaryResponse {
  totalAssets: number;
  todayExpense: number;
  businessDate: string;
  accounts: AccountSummaryDto[];
  recentTransactions: RecentTransactionDto[];
}
