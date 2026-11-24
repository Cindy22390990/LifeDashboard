import type {
  AccountSummaryViewModel,
  BudgetRadarViewModel,
  DashboardViewModel,
  GoalProgressViewModel,
  RecentTransactionItemViewModel,
} from '../../../application/dashboard/models/DashboardViewModel';
import { AccountType } from '../../../application/dashboard/models/AccountType';
import { TransactionType } from '../../../application/dashboard/models/TransactionType';
import type { MockDashboardData, MockTransaction } from '../mock/mockDashboardData';

const categoryTranslationKeys: Record<MockTransaction['category'], string> = {
  income: 'transactions.categories.income',
  dining: 'transactions.categories.dining',
  groceries: 'transactions.categories.groceries',
  transfer: 'transactions.categories.transfer',
};

function mapTransaction(transaction: MockTransaction): RecentTransactionItemViewModel {
  return {
    id: transaction.id,
    title: transaction.title,
    subtitle: transaction.note,
    categoryKey: categoryTranslationKeys[transaction.category],
    accountName: transaction.accountName,
    transactionType: mapTransactionType(transaction.transactionType),
    amount: transaction.amount,
    occurredAt: transaction.occurredAt,
    icon: transaction.icon,
  };
}

function mapTransactionType(type: MockTransaction['transactionType']): TransactionType {
  switch (type) {
    case 'income':
      return TransactionType.Income;
    case 'expense':
      return TransactionType.Expense;
    case 'transfer':
      return TransactionType.Transfer;
    default:
      return TransactionType.Unknown;
  }
}

function mapAccountType(type: string | null): AccountType {
  switch (type?.toLowerCase()) {
    case 'bank':
    case 'checking':
      return AccountType.Bank;
    case 'cash':
      return AccountType.Cash;
    case 'creditcard':
      return AccountType.CreditCard;
    case 'investment':
      return AccountType.Investment;
    default:
      return AccountType.Unknown;
  }
}

function mapBudgetRadar(data: MockDashboardData['budgetRadar']): BudgetRadarViewModel {
  return {
    ...data,
    usagePercentage: data.totalBudget > 0
      ? Math.round((data.totalSpent / data.totalBudget) * 100)
      : 0,
  };
}

function mapGoalProgress(
  goal: MockDashboardData['goals'][number],
): GoalProgressViewModel {
  const remainingAmount = Math.max(0, goal.targetAmount - goal.savedAmount);
  const progressPercentage = goal.targetAmount > 0
    ? Math.min(100, Math.round((goal.savedAmount / goal.targetAmount) * 100))
    : 0;

  return {
    goalId: goal.id,
    name: goal.name,
    targetAmount: goal.targetAmount,
    fundedAmount: goal.savedAmount,
    remainingAmount,
    progressPercentage,
    deadlineDate: goal.deadlineDate,
    icon: goal.icon,
  };
}

function mapAccountSummary(
  account: MockDashboardData['accounts'][number],
): AccountSummaryViewModel {
  return {
    accountId: account.id,
    name: account.name,
    balance: account.balance,
    accountType: mapAccountType(account.type),
  };
}

export function mapDashboardData(data: MockDashboardData): DashboardViewModel {
  const budgetRadar = mapBudgetRadar(data.budgetRadar);
  const goalProgress = data.goals.map(mapGoalProgress);
  const recentTransactions = data.transactions.slice(0, 5).map(mapTransaction);
  const accountSummary = data.accounts.map(mapAccountSummary);

  return {
    meta: {
      businessDate: data.referenceDate,
    },
    referenceDate: data.referenceDate,
    heroStats: data.heroStats,
    budgetRadar: { status: 'ready', data: budgetRadar },
    goalProgress: goalProgress.length > 0
      ? { status: 'ready', data: goalProgress }
      : { status: 'empty' },
    recentTransactions: recentTransactions.length > 0
      ? { status: 'ready', data: recentTransactions }
      : { status: 'empty' },
    accountSummary: accountSummary.length > 0
      ? { status: 'ready', data: accountSummary }
      : { status: 'empty' },
  };
}
