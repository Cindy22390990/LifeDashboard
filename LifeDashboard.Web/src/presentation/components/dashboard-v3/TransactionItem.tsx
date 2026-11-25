import type { RecentTransactionItemViewModel } from '../../../application/dashboard/models/DashboardViewModel';
import { formatDate, formatSensitiveMoney } from '../../../i18n/formatters';
import { useTranslation } from 'react-i18next';
import { usePrivacyDisplayStore } from '../../stores/privacyDisplayStore';
import './TransactionItem.css';

interface TransactionItemProps {
  locale: string;
  viewModel: RecentTransactionItemViewModel;
}

export default function TransactionItem({
  locale,
  viewModel,
}: TransactionItemProps) {
  const { t } = useTranslation();
  const isAmountHidden = usePrivacyDisplayStore((state) => state.isAmountHidden);
  const title = viewModel.title || (
    viewModel.transactionType === 'unknown'
      ? t('transactions.types.unknown')
      : t(viewModel.categoryKey)
  );

  return (
    <li className={`v3-transaction-item v3-transaction-item--${viewModel.transactionType}`}>
      <span className="v3-transaction-item__icon" aria-hidden="true">
        {viewModel.icon}
      </span>
      <div className="v3-transaction-item__content">
        <div className="v3-transaction-item__title-row">
          <strong>{title}</strong>
          <strong className="v3-transaction-item__amount">
            {formatSensitiveMoney(viewModel.amount, locale, isAmountHidden, {
              amountSign: viewModel.transactionType === 'income'
                ? 'positive'
                : viewModel.transactionType === 'expense'
                  ? 'negative'
                  : undefined,
            })}
          </strong>
        </div>
        {viewModel.subtitle && (
          <span className="v3-transaction-item__subtitle">{viewModel.subtitle}</span>
        )}
        <div className="v3-transaction-item__metadata">
          <span>
            {viewModel.transactionType === 'unknown'
              ? t('transactions.types.unknown')
              : t(viewModel.categoryKey)}
          </span>
          {viewModel.transactionType === 'unknown' && (
            <>
              <span aria-hidden="true">·</span>
              <span>{t(viewModel.categoryKey)}</span>
            </>
          )}
          <span aria-hidden="true">·</span>
          <span>{viewModel.accountName}</span>
          <time dateTime={viewModel.occurredAt}>
            {formatDate(viewModel.occurredAt, locale, { month: 'short', day: '2-digit' })}
          </time>
        </div>
      </div>
    </li>
  );
}
