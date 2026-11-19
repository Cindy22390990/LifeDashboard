import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { formatDate, formatSensitiveMoney, type MoneyAmount } from '../../../i18n/formatters';
import type { MockTransaction } from './mockDashboardData';
import { toTransactionItemViewModel } from './transactionItemAdapter';
import TransactionItem from './TransactionItem';
import './DayDetailPanel.css';

interface DayDetailPanelProps {
  date: string;
  isAmountHidden: boolean;
  locale: string;
  isMobileSheet?: boolean;
  onClose?: () => void;
  transactions: readonly MockTransaction[];
  total: MoneyAmount;
}

export default function DayDetailPanel({
  date,
  isAmountHidden,
  locale,
  isMobileSheet = false,
  onClose,
  transactions,
  total,
}: DayDetailPanelProps) {
  const { t } = useTranslation();
  const [isAddTransactionOpen, setIsAddTransactionOpen] = useState(false);
  const [transactionType, setTransactionType] = useState('Expense');

  return (
    <>
      <section
        aria-labelledby="v3-day-detail-title"
        className={`v3-day-detail-panel${isMobileSheet ? ' v3-day-detail-panel--sheet' : ''}`}
      >
        <div className="v3-day-detail-panel__heading">
          <div>
            <p className="v3-section-eyebrow">{t('calendar.dayDetails')}</p>
            <h3 id="v3-day-detail-title">
              {formatDate(date, locale, {
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
        <div className="v3-day-detail-panel__total">
          <span>{t('calendar.total')}</span>
          <strong>{formatSensitiveMoney(total, locale, isAmountHidden)}</strong>
        </div>
        {transactions.length > 0 ? (
          <ul className="v3-day-detail-panel__transactions">
            {transactions.map((transaction) => (
              <TransactionItem
                key={transaction.id}
                isAmountHidden={isAmountHidden}
                locale={locale}
                transaction={toTransactionItemViewModel(transaction, locale)}
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
          onClick={() => setIsAddTransactionOpen(true)}
          type="button"
        >
          + {t('calendar.addTransaction')}
        </button>
      </section>

      {isAddTransactionOpen && (
        <div
          className="v3-mock-modal-backdrop"
          onMouseDown={(event) => {
            if (event.target === event.currentTarget) {
              setIsAddTransactionOpen(false);
            }
          }}
        >
          <section
            aria-labelledby="v3-mock-transaction-title"
            aria-modal="true"
            className="v3-mock-transaction-modal"
            role="dialog"
          >
            <div className="v3-mock-transaction-modal__heading">
              <h3 id="v3-mock-transaction-title">{t('transactions.add')}</h3>
              <button
                aria-label={t('transactions.closePreview')}
                onClick={() => setIsAddTransactionOpen(false)}
                type="button"
              >
                ×
              </button>
            </div>
            <p className="v3-mock-transaction-modal__note">
              {t('common.previewOnly')}
            </p>
            <label>
              {t('transactions.date')}
              <input type="date" value={date} readOnly />
            </label>
            <label>
              {t('transactions.type')}
              <select
                onChange={(event) => setTransactionType(event.target.value)}
                value={transactionType}
              >
                <option value="Income">{t('transactions.types.income')}</option>
                <option value="Expense">{t('transactions.types.expense')}</option>
                <option value="Transfer">{t('transactions.types.transfer')}</option>
              </select>
            </label>
            <label>
              {t('transactions.title')}
              <input placeholder={t('transactions.title')} type="text" />
            </label>
            <label>
              {t('transactions.amount')}
              <input min="0" placeholder="0" type="number" />
            </label>
            <button
              className="v3-button v3-button--primary"
              onClick={() => setIsAddTransactionOpen(false)}
              type="button"
            >
              {t('common.closePreview')}
            </button>
          </section>
        </div>
      )}
    </>
  );
}
