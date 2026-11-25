import { useTranslation } from 'react-i18next';
import type { CalendarDayDetailViewModel } from '../../../application/dashboard/models/DashboardViewModel';
import { formatDate, formatSensitiveMoney } from '../../../i18n/formatters';
import { usePrivacyDisplayStore } from '../../stores/privacyDisplayStore';
import TransactionItem from './TransactionItem';
import './DayDetailPanel.css';

interface DayDetailPanelProps {
  locale: string;
  isMobileSheet?: boolean;
  onClose?: () => void;
  onAddTransaction: (date: string) => void;
  viewModel: CalendarDayDetailViewModel;
}

export default function DayDetailPanel({
  locale,
  isMobileSheet = false,
  onClose,
  onAddTransaction,
  viewModel,
}: DayDetailPanelProps) {
  const { t } = useTranslation();
  const isAmountHidden = usePrivacyDisplayStore((state) => state.isAmountHidden);

  return (
    <section
        aria-labelledby="v3-day-detail-title"
        className={`v3-day-detail-panel${isMobileSheet ? ' v3-day-detail-panel--sheet' : ''}`}
      >
        <div className="v3-day-detail-panel__heading">
          <div>
            <p className="v3-section-eyebrow">{t('calendar.dayDetails')}</p>
            <h3 id="v3-day-detail-title">
              {formatDate(viewModel.date, locale, {
                month: 'short',
                day: 'numeric',
                year: 'numeric',
              })}
            </h3>
          </div>
          {isMobileSheet && (
            <button
              aria-label={t('calendar.closeDetails')}
              className="v3-day-detail-panel__close"
              onClick={onClose}
              type="button"
            >
              ×
            </button>
          )}
        </div>
        <div className="v3-day-detail-panel__totals">
          <div>
            <span>{t('calendar.income')}</span>
            <strong>{formatSensitiveMoney(viewModel.totalIncome, locale, isAmountHidden)}</strong>
          </div>
          <div>
            <span>{t('calendar.expense')}</span>
            <strong>{formatSensitiveMoney(viewModel.totalExpense, locale, isAmountHidden)}</strong>
          </div>
        </div>
        {viewModel.status === 'ready' ? (
          <ul className="v3-day-detail-panel__transactions">
            {viewModel.transactions.map((transaction) => (
              <TransactionItem
                key={transaction.id}
                locale={locale}
                viewModel={transaction}
              />
            ))}
          </ul>
        ) : (
          <div className="v3-day-detail-panel__empty">
            <span aria-hidden="true">◇</span>
            <p>{t('calendar.empty')}</p>
          </div>
        )}
        <button
          className="v3-button v3-button--primary v3-day-detail-panel__add"
          onClick={() => onAddTransaction(viewModel.date)}
          type="button"
        >
          + {t('calendar.addTransaction')}
        </button>
      </section>
  );
}
