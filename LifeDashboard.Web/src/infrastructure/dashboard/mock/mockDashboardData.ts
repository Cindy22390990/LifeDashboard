export interface MockTransaction {
  id: string;
  title: string;
  note: string | null;
  category: 'income' | 'dining' | 'groceries' | 'transfer';
  accountName: string;
  transactionType: 'income' | 'expense' | 'transfer';
  amount: number;
  occurredAt: string;
  icon: string;
}

export interface MockDashboardData {
  referenceDate: string;
  heroStats: {
    totalAssets: number;
    spentToday: number;
    remainingBudget: number;
    monthlyIncome: number;
    monthlyExpense: number;
  };
  budgetRadar: {
    periodKey: string;
    totalBudget: number;
    totalSpent: number;
    remainingLivingBudget: number;
    healthStatus: 'normal' | 'warning' | 'exceeded' | 'unavailable';
  };
  goals: {
    id: string;
    name: string;
    targetAmount: number;
    savedAmount: number;
    deadlineDate: string | null;
    icon: string;
  }[];
  transactions: MockTransaction[];
  accounts: {
    id: string;
    name: string;
    balance: number;
    type: string | null;
  }[];
}

export const mockDashboardData: MockDashboardData = {
  referenceDate: '2026-11-18',
  heroStats: {
    totalAssets: 125000,
    spentToday: 350,
    remainingBudget: 12400,
    monthlyIncome: 45000,
    monthlyExpense: 800,
  },
  budgetRadar: {
    periodKey: 'budget.currentCycle',
    totalBudget: 35000,
    totalSpent: 22600,
    remainingLivingBudget: 12400,
    healthStatus: 'normal',
  },
  goals: [
    {
      id: 'japan-trip',
      name: 'Japan Trip',
      targetAmount: 100000,
      savedAmount: 65000,
      deadlineDate: '2027-12-01',
      icon: '✈',
    },
    {
      id: 'emergency-fund',
      name: 'Emergency Fund',
      targetAmount: 200000,
      savedAmount: 84000,
      deadlineDate: '2027-06-01',
      icon: '◇',
    },
    {
      id: 'home-upgrade',
      name: 'Home Upgrade',
      targetAmount: 200000,
      savedAmount: 56000,
      deadlineDate: '2028-12-01',
      icon: '⌂',
    },
  ],
  transactions: [
    {
      id: 'transaction-1',
      title: 'Salary',
      note: 'November salary',
      category: 'income',
      accountName: 'Hua Nan Bank',
      transactionType: 'income',
      amount: 45000,
      occurredAt: '2026-11-05T00:00:00',
      icon: '↗',
    },
    {
      id: 'transaction-2',
      title: 'Starbucks',
      note: 'Coffee before work',
      category: 'dining',
      accountName: 'Hua Nan Bank',
      transactionType: 'expense',
      amount: 150,
      occurredAt: '2026-11-15T00:00:00',
      icon: '☕',
    },
    {
      id: 'transaction-3',
      title: 'Groceries',
      note: 'Weekly shopping',
      category: 'groceries',
      accountName: 'Cash',
      transactionType: 'expense',
      amount: 300,
      occurredAt: '2026-11-15T00:00:00',
      icon: '▤',
    },
    {
      id: 'transaction-4',
      title: 'Lunch',
      note: 'Lunch with team',
      category: 'dining',
      accountName: 'Hua Nan Bank',
      transactionType: 'expense',
      amount: 350,
      occurredAt: '2026-11-18T00:00:00',
      icon: '◇',
    },
    {
      id: 'transaction-5',
      title: 'Cash transfer',
      note: 'Move money to wallet',
      category: 'transfer',
      accountName: 'Hua Nan Bank → Cash',
      transactionType: 'transfer',
      amount: 5000,
      occurredAt: '2026-11-17T00:00:00',
      icon: '⇄',
    },
  ],
  accounts: [
    {
      id: 'account-1',
      name: 'Hua Nan Bank',
      balance: 80000,
      type: 'checking',
    },
    {
      id: 'account-2',
      name: 'Cash',
      balance: 5000,
      type: 'cash',
    },
    {
      id: 'account-3',
      name: 'Savings Account',
      balance: 40000,
      type: 'savings',
    },
  ],
};
