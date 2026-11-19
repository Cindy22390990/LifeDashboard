import type { MoneyAmount } from '../../../i18n/formatters';

export type MockTransactionType = 'Income' | 'Expense' | 'Transfer';

export interface MockTransaction {
  id: string;
  title: string;
  subtitle: string;
  category: string;
  account: string;
  date: string;
  amount: MoneyAmount;
  type: MockTransactionType;
  icon: string;
}

export const mockDashboardData = {
  currentDate: '2026-11-18',
  heroStats: [
    {
      id: 'assets',
      amount: { amount: 125000, currency: 'TWD' },
      labelKey: 'dashboard.hero.totalAssets',
      captionKey: 'dashboard.hero.netWorth',
      icon: '◇',
      emphasis: 'primary',
    },
    {
      id: 'spent-today',
      amount: { amount: 350, currency: 'TWD' },
      labelKey: 'dashboard.hero.spentToday',
      captionKey: 'dashboard.hero.today',
      icon: '↓',
      emphasis: 'standard',
    },
    {
      id: 'remaining-budget',
      amount: { amount: 12400, currency: 'TWD' },
      labelKey: 'dashboard.hero.leftToSpend',
      captionKey: 'dashboard.hero.budgetCycle',
      icon: '○',
      emphasis: 'positive',
    },
  ] as const,
  budget: {
    statusKey: 'budget.safe',
    usedPercent: 65,
    remainingAmount: { amount: 12400, currency: 'TWD' },
    periodKey: 'budget.currentCycle',
  },
  goals: [
    {
      id: 'japan-trip',
      name: 'Japan Trip',
      icon: '✈',
      progressPercent: 65,
      savedAmount: { amount: 65000, currency: 'TWD' },
      targetAmount: { amount: 100000, currency: 'TWD' },
      remainingAmount: { amount: 35000, currency: 'TWD' },
      deadline: '2027-12-01',
    },
    {
      id: 'emergency-fund',
      name: 'Emergency Fund',
      icon: '◇',
      progressPercent: 42,
      savedAmount: { amount: 84000, currency: 'TWD' },
      targetAmount: { amount: 200000, currency: 'TWD' },
      remainingAmount: { amount: 116000, currency: 'TWD' },
      deadline: '2027-06-01',
    },
    {
      id: 'home-upgrade',
      name: 'Home Upgrade',
      icon: '⌂',
      progressPercent: 28,
      savedAmount: { amount: 56000, currency: 'TWD' },
      targetAmount: { amount: 200000, currency: 'TWD' },
      remainingAmount: { amount: 144000, currency: 'TWD' },
      deadline: '2028-12-01',
    },
  ],
  transactions: [
    {
      id: 'transaction-1',
      title: 'Salary',
      subtitle: 'November salary',
      category: 'Income',
      account: 'Hua Nan Bank',
      date: '2026-11-05',
      amount: { amount: 45000, currency: 'TWD' },
      type: 'Income',
      icon: '↗',
    },
    {
      id: 'transaction-2',
      title: 'Starbucks',
      subtitle: 'Coffee before work',
      category: 'Dining',
      account: 'Hua Nan Bank',
      date: '2026-11-15',
      amount: { amount: -150, currency: 'TWD' },
      type: 'Expense',
      icon: '☕',
    },
    {
      id: 'transaction-3',
      title: 'Groceries',
      subtitle: 'Weekly shopping',
      category: 'Groceries',
      account: 'Cash',
      date: '2026-11-15',
      amount: { amount: -300, currency: 'TWD' },
      type: 'Expense',
      icon: '▤',
    },
    {
      id: 'transaction-4',
      title: 'Lunch',
      subtitle: 'Lunch with team',
      category: 'Dining',
      account: 'Hua Nan Bank',
      date: '2026-11-18',
      amount: { amount: -350, currency: 'TWD' },
      type: 'Expense',
      icon: '◇',
    },
    {
      id: 'transaction-5',
      title: 'Cash transfer',
      subtitle: 'Move money to wallet',
      category: 'Transfer',
      account: 'Hua Nan Bank → Cash',
      date: '2026-11-17',
      amount: { amount: 5000, currency: 'TWD' },
      type: 'Transfer',
      icon: '⇄',
    },
  ] satisfies readonly MockTransaction[],
  calendarMarkers: {
    '2026-11-05': 'income',
    '2026-11-15': 'expense',
    '2026-11-17': 'transfer',
    '2026-11-18': 'expense',
  } as const,
  dailyTotals: {
    '2026-11-05': { amount: 45000, currency: 'TWD' },
    '2026-11-15': { amount: -450, currency: 'TWD' },
    '2026-11-17': { amount: 5000, currency: 'TWD' },
    '2026-11-18': { amount: -350, currency: 'TWD' },
  } as const,
  accounts: [
    {
      id: 'account-1',
      name: 'Hua Nan Bank',
      type: 'checking',
      balance: { amount: 80000, currency: 'TWD' },
    },
    {
      id: 'account-2',
      name: 'Cash',
      type: 'cash',
      balance: { amount: 5000, currency: 'TWD' },
    },
    {
      id: 'account-3',
      name: 'Savings Account',
      type: 'savings',
      balance: { amount: 40000, currency: 'TWD' },
    },
  ],
} as const;
