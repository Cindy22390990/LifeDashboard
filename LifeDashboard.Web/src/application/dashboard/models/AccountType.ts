export const AccountType = {
  Bank: 'bank',
  Cash: 'cash',
  CreditCard: 'creditCard',
  Investment: 'investment',
  Unknown: 'unknown',
} as const;

export type AccountType = typeof AccountType[keyof typeof AccountType];
