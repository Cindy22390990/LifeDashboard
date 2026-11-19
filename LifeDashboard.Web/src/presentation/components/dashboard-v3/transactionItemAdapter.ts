import type { MockTransaction } from './mockDashboardData';
import { formatDate, type MoneyAmount } from '../../../i18n/formatters';

export interface TransactionItemViewModel {
  id: string;
  title: string;
  subtitle: string;
  category: string;
  account: string;
  date: string;
  amount: MoneyAmount;
  type: MockTransaction['type'];
  icon: string;
}

export function toTransactionItemViewModel(
  transaction: MockTransaction,
  locale: string,
): TransactionItemViewModel {
  return {
    id: transaction.id,
    title: transaction.title,
    subtitle: transaction.subtitle,
    category: transaction.category,
    account: transaction.account,
    date: formatDate(transaction.date, locale, { month: 'short', day: '2-digit' }),
    amount: transaction.amount,
    type: transaction.type,
    icon: transaction.icon,
  };
}
