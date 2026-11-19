import { mockDashboardData } from './mockDashboardData';
import { usePrivacyDisplayStore } from '../../stores/privacyDisplayStore';
import TransactionItem from './TransactionItem';
import { toTransactionItemViewModel } from './transactionItemAdapter';
import { useTranslation } from 'react-i18next';
import './RecentTransactionsWidget.css';

export default function RecentTransactionsWidget() {
  const { t, i18n } = useTranslation();
  const locale = i18n.resolvedLanguage ?? i18n.language;
  const isAmountHidden = usePrivacyDisplayStore((state) => state.isAmountHidden);
  const recentTransactions = mockDashboardData.transactions.slice(0, 5);

  return (
    <section className="v3-widget v3-recent-transactions" aria-labelledby="v3-recent-title">
      <div className="v3-widget__heading">
        <div>
          <p className="v3-section-eyebrow">{t('calendar.activity')}</p>
          <h2 id="v3-recent-title">{t('transactions.recentTitle')}</h2>
        </div>
        <button className="v3-text-button" type="button">{t('common.viewAll')}</button>
      </div>
      <ul className="v3-recent-transactions__list">
        {recentTransactions.map((transaction) => (
          <TransactionItem
            key={transaction.id}
            isAmountHidden={isAmountHidden}
            locale={locale}
            transaction={toTransactionItemViewModel(transaction, locale)}
          />
        ))}
      </ul>
    </section>
  );
}
