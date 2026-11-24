import type { AccountType } from './AccountType';
import type { SectionState } from './SectionState';
import type { TransactionType } from './TransactionType';

export type DashboardTransactionType = TransactionType;
export type BudgetHealthStatus = 'normal' | 'warning' | 'exceeded' | 'unavailable';
export type CalendarActivityMarker = 'income' | 'expense' | 'transfer';

export interface HeroStatsViewModel {
  totalAssets: number;
  spentToday: number;
  remainingBudget: number | null;
  monthlyIncome: number | null;
  monthlyExpense: number | null;
}

export interface RecentTransactionItemViewModel {
  id: string;
  title: string;
  subtitle: string | null;
  categoryKey: string;
  accountName: string;
  transactionType: TransactionType;
  amount: number;
  occurredAt: string;
  icon: string;
}

export interface CalendarDaySummaryViewModel {
  date: string;
  totalIncome: number;
  totalExpense: number;
  hasTransfer: boolean;
  marker?: CalendarActivityMarker;
}

export interface CalendarDayDetailViewModel {
  date: string;
  totalIncome: number;
  totalExpense: number;
  transactions: RecentTransactionItemViewModel[];
  status: 'ready' | 'empty';
}

export interface CalendarMonthViewModel {
  year: number;
  month: number;
  daysByDate: Readonly<Record<string, CalendarDaySummaryViewModel>>;
}

export interface BudgetRadarViewModel {
  periodKey: string;
  totalBudget: number;
  totalSpent: number;
  remainingLivingBudget: number;
  usagePercentage: number;
  healthStatus: BudgetHealthStatus;
}

export interface GoalProgressViewModel {
  goalId: string;
  name: string;
  targetAmount: number;
  fundedAmount: number;
  remainingAmount: number;
  progressPercentage: number;
  deadlineDate: string | null;
  icon: string;
}

export interface AccountSummaryViewModel {
  accountId: string;
  name: string;
  balance: number;
  accountType: AccountType;
}

export interface DashboardViewModel {
  meta: {
    businessDate: string;
  };
  referenceDate: string;
  heroStats: HeroStatsViewModel;
  budgetRadar: SectionState<BudgetRadarViewModel>;
  goalProgress: SectionState<GoalProgressViewModel[]>;
  recentTransactions: SectionState<RecentTransactionItemViewModel[]>;
  accountSummary: SectionState<AccountSummaryViewModel[]>;
}
