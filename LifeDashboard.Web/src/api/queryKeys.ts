import type { TransactionListFilter } from './dashboard-v4/dto/transactionDto';

export const dashboardV4Keys = {
  all: ['v4'] as const,
  home: () => [...dashboardV4Keys.all, 'home'] as const,
  transactions: {
    all: () => ['transactions'] as const,
    list: (filter: TransactionListFilter) => ['transactions', 'list', filter] as const,
    detail: (id: string) => ['transactions', 'detail', id] as const,
  },
  me: {
    assets: () => ['me', 'assets'] as const,
  },
  accounts: {
    all: () => ['accounts'] as const,
  },
  categories: {
    all: () => ['categories'] as const,
  },
  plan: {
    budget: (year?: number, month?: number) =>
      ['budgets', 'current', { year, month }] as const,
    goals: () => ['goals', 'summary'] as const,
  },
};
