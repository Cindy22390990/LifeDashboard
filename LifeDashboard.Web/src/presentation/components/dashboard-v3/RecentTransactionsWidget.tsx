import type { RecentTransactionItemViewModel } from '../../../application/dashboard/models/DashboardViewModel';
import type { SectionState } from '../../../application/dashboard/models/SectionState';
import TransactionItem from './TransactionItem';
import SectionStateMessage from './SectionStateMessage';
import { useTranslation } from 'react-i18next';
import './RecentTransactionsWidget.css';

interface RecentTransactionsWidgetProps {
  state: SectionState<RecentTransactionItemViewModel[]>;
}

export default function RecentTransactionsWidget({
  state,
}: RecentTransactionsWidgetProps) {
  const { t, i18n } = useTranslation();
  const locale = i18n.resolvedLanguage ?? i18n.language;
  return (
    <section className="v3-widget v3-recent-transactions" aria-labelledby="v3-recent-title">
      <div className="v3-widget__heading">
        <div>
          <p className="v3-section-eyebrow">{t('calendar.activity')}</p>
          <h2 id="v3-recent-title">{t('transactions.recentTitle')}</h2>
        </div>
        <button className="v3-text-button" type="button">{t('common.viewAll')}</button>
      </div>
      {state.status === 'ready' ? (
        <ul className="v3-recent-transactions__list">
          {state.data.map((transaction) => (
            <TransactionItem
              key={transaction.id}
              locale={locale}
              viewModel={transaction}
            />
          ))}
        </ul>
      ) : (
        <SectionStateMessage emptyMessage={t('transactions.empty')} state={state} />
      )}
    </section>
  );
}
