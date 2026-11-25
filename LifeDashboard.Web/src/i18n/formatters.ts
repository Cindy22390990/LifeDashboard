export interface MoneyAmount {
  amount: number;
  currency: string;
}

export interface CurrencyFormatOptions {
  signDisplay?: Intl.NumberFormatOptions['signDisplay'];
  amountSign?: 'positive' | 'negative';
}

export function formatCurrency(
  value: MoneyAmount | number,
  locale: string,
  options: CurrencyFormatOptions = {},
): string {
  const money = typeof value === 'number'
    ? { amount: value, currency: 'TWD' }
    : value;
  const amount = options.amountSign === 'negative'
    ? -Math.abs(money.amount)
    : options.amountSign === 'positive'
      ? Math.abs(money.amount)
      : money.amount;

  return new Intl.NumberFormat(locale, {
    style: 'currency',
    currency: money.currency,
    signDisplay: options.amountSign === 'positive'
      ? 'exceptZero'
      : options.signDisplay,
  }).format(amount);
}

export function formatSensitiveMoney(
  value: MoneyAmount | number,
  locale: string,
  isAmountHidden: boolean,
  options: CurrencyFormatOptions = {},
): string {
  const money = typeof value === 'number'
    ? { amount: value, currency: 'TWD' }
    : value;

  return isAmountHidden ? '••••••' : formatCurrency(money, locale, options);
}

export function formatDate(
  value: string | Date,
  locale: string,
  options: Intl.DateTimeFormatOptions = {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  },
): string {
  let date: Date;

  if (typeof value === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(value)) {
    const [year, month, day] = value.split('-').map(Number);
    date = new Date(year, month - 1, day);
  } else {
    date = new Date(value);
  }

  return new Intl.DateTimeFormat(locale, options).format(date);
}
