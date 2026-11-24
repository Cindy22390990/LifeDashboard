export const TransactionType = {
  Income: 'income',
  Expense: 'expense',
  Transfer: 'transfer',
  Unknown: 'unknown',
} as const;

export type TransactionType = typeof TransactionType[keyof typeof TransactionType];
