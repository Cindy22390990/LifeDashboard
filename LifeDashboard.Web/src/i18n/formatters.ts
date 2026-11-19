export interface MoneyAmount {
  amount: number;
  currency: string;
}

export interface CurrencyFormatOptions {
  signDisplay?: Intl.NumberFormatOptions['signDisplay'];
}

export function formatCurrency(
  value: MoneyAmount,
  locale: string,
  options: CurrencyFormatOptions = {},
): string {
  return new Intl.NumberFormat(locale, {
    style: 'currency',
    currency: value.currency,
    signDisplay: options.signDisplay,
  }).format(value.amount);
}

export function formatSensitiveMoney(
  value: MoneyAmount,
  locale: string,
  isAmountHidden: boolean,
  options: CurrencyFormatOptions = {},
): string {
  if (!isAmountHidden) {
    return formatCurrency(value, locale, options);
  }

  return new Intl.NumberFormat(locale, {
    style: 'currency',
    currency: value.currency,
  })
    .formatToParts(0)
    .map((part) => {
      if (part.type === 'integer') return '••••••';
      if (['group', 'decimal', 'fraction'].includes(part.type)) return '';
      return part.value;
    })
    .join('');
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
  const date = typeof value === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(value)
    ? new Date(`${value}T00:00:00`)
    : new Date(value);

  return new Intl.DateTimeFormat(locale, options).format(date);
}
