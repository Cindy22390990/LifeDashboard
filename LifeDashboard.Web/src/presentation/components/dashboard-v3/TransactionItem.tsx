import type { TransactionItemViewModel } from './transactionItemAdapter';
import { formatSensitiveMoney } from '../../../i18n/formatters';
import { useTranslation } from 'react-i18next';
import './TransactionItem.css';

interface TransactionItemProps {
  isAmountHidden: boolean;
  locale: string;
  transaction: TransactionItemViewModel;
}

export default function TransactionItem({
  isAmountHidden,
  locale,
  transaction,
}: TransactionItemProps) {
  const { t } = useTranslation();
  const categoryKeys: Record<string, string> = {
    Income: 'transactions.categories.income',
    Dining: 'transactions.categories.dining',
    Groceries: 'transactions.categories.groceries',
    Transfer: 'transactions.categories.transfer',
  };
  const categoryKey = categoryKeys[transaction.category];
  const category = categoryKey
    ? t(categoryKey)
    : transaction.category;

  return (
    <li className={`v3-transaction-item v3-transaction-item--${transaction.type.toLowerCase()}`}>
      <span className="v3-transaction-item__icon" aria-hidden="true">
        {transaction.icon}
      </span>
      <div className="v3-transaction-item__content">
        <div className="v3-transaction-item__title-row">
          <strong>{transaction.title}</strong>
          <strong className="v3-transaction-item__amount">
            {formatSensitiveMoney(transaction.amount, locale, isAmountHidden, {
              signDisplay: transaction.type === 'Income' ? 'exceptZero' : 'auto',
            })}
          </strong>
        </div>
        {transaction.subtitle && (
          <span className="v3-transaction-item__subtitle">{transaction.subtitle}</span>
        )}
        <div className="v3-transaction-item__metadata">
          <span>{category}</span>
          <span aria-hidden="true">·</span>
          <span>{transaction.account}</span>
          <time dateTime={transaction.date}>{transaction.date}</time>
        </div>
      </div>
    </li>
  );
}
