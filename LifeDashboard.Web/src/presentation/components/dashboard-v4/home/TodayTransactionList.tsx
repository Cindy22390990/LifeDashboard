import { useTranslation } from 'react-i18next';
import { formatCurrency, formatDate } from '../../../../i18n/formatters';
import type { V4HomeViewModel } from '../../../../mappers/dashboardV4';

interface TodayTransactionListProps {
  viewModel: Pick<V4HomeViewModel, 'todayTransactions'>;
}

export function TodayTransactionList({
  viewModel,
}: TodayTransactionListProps) {
  const { t, i18n } = useTranslation();
  const locale = i18n.resolvedLanguage ?? i18n.language;

  return (
    <section
      aria-labelledby="v4-today-transactions-title"
      className="v4-home-card v4-today-transactions"
    >
      <h2 id="v4-today-transactions-title">{t('home.todayTransactions')}</h2>
      {viewModel.todayTransactions.length === 0 ? (
        <p className="v4-home__empty" role="status">
          {t('transactions.empty')}
        </p>
      ) : (
        <ul className="v4-today-transactions__list">
          {viewModel.todayTransactions.map((transaction) => (
            <li className="v4-today-transactions__item" key={transaction.id}>
              <div>
                <p className="v4-today-transactions__title">
                  {transaction.title}
                </p>
                <p className="v4-today-transactions__meta">
                  {t(`transactions.types.${transaction.transactionType}`)}
                  {' · '}
                  {formatDate(transaction.transactionDate.isoDate, locale)}
                </p>
              </div>
              <strong>
                {formatCurrency(
                  transaction.amount,
                  locale,
                  {
                    amountSign: transaction.transactionType === 'expense'
                      ? 'negative'
                      : transaction.transactionType === 'income'
                        ? 'positive'
                        : undefined,
                  },
                )}
              </strong>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
