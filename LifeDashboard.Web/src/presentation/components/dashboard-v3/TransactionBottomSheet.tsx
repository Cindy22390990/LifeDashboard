import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useAccountStore } from '../../../application/stores/accountStore';
import {
  TransactionType,
  type ITransaction,
  type TransactionType as TransactionTypeValue,
} from '../../../domain/Transaction';
import './TransactionBottomSheet.css';

interface TransactionBottomSheetProps {
  isOpen: boolean;
  defaultDate: string;
  onClose: () => void;
  onSuccess?: (transaction: ITransaction) => void;
}

export default function TransactionBottomSheet({
  isOpen,
  defaultDate,
  onClose,
  onSuccess: _onSuccess,
}: TransactionBottomSheetProps) {
  const { t } = useTranslation();
  const accounts = useAccountStore((state) => state.accounts);
  const isAccountLoading = useAccountStore((state) => state.isLoading);
  const fetchAccounts = useAccountStore((state) => state.fetchAccounts);
  const [transactionType, setTransactionType] = useState<TransactionTypeValue>(
    TransactionType.Expense,
  );
  const [amount, setAmount] = useState('');
  const [accountId, setAccountId] = useState('');
  const [transactionDate, setTransactionDate] = useState(defaultDate);
  const [note, setNote] = useState('');
  const hasRequestedAccounts = useRef(false);

  useEffect(() => {
    if (!isOpen) return;

    setTransactionDate(defaultDate);
    setAmount('');
    setNote('');
    setAccountId('');
    setTransactionType(TransactionType.Expense);

    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onClose();
      }
    };

    document.addEventListener('keydown', handleEscape);
    return () => document.removeEventListener('keydown', handleEscape);
  }, [isOpen, defaultDate, onClose]);

  useEffect(() => {
    if (!isOpen) {
      hasRequestedAccounts.current = false;
      return;
    }

    if (
      accounts.length === 0 &&
      !isAccountLoading &&
      !hasRequestedAccounts.current
    ) {
      hasRequestedAccounts.current = true;
      void fetchAccounts();
    }
  }, [isOpen, accounts.length, isAccountLoading, fetchAccounts]);

  useEffect(() => {
    if (accountId && accounts.some((account) => account.id === accountId)) {
      return;
    }

    setAccountId(accounts[0]?.id ?? '');
  }, [accounts, accountId]);

  if (!isOpen) return null;

  return (
    <div
      className="v3-transaction-sheet-backdrop"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) {
          onClose();
        }
      }}
    >
      <section
        aria-labelledby="v3-transaction-sheet-title"
        aria-modal="true"
        className="v3-transaction-sheet"
        role="dialog"
      >
        <div className="v3-transaction-sheet__header">
          <div>
            <p className="v3-transaction-sheet__eyebrow">
              {defaultDate}
            </p>
            <h2 id="v3-transaction-sheet-title">
              {t('transactions.add')}
            </h2>
          </div>
          <button
            aria-label={t('transactions.closePreview')}
            className="v3-transaction-sheet__close"
            onClick={onClose}
            type="button"
          >
            ×
          </button>
        </div>
        <div className="v3-transaction-sheet__form">
          <div
            aria-label={t('transactions.type')}
            className="v3-transaction-sheet__type-toggle"
          >
            <button
              aria-pressed={transactionType === TransactionType.Expense}
              className={transactionType === TransactionType.Expense
                ? 'is-active'
                : ''}
              onClick={() => setTransactionType(TransactionType.Expense)}
              type="button"
            >
              {t('transactions.types.expense')}
            </button>
            <button
              aria-pressed={transactionType === TransactionType.Income}
              className={transactionType === TransactionType.Income
                ? 'is-active'
                : ''}
              onClick={() => setTransactionType(TransactionType.Income)}
              type="button"
            >
              {t('transactions.types.income')}
            </button>
          </div>

          <label className="v3-transaction-sheet__field">
            <span>{t('transactions.amount')}</span>
            <input
              inputMode="decimal"
              min="0"
              onChange={(event) => setAmount(event.target.value)}
              placeholder="0"
              type="number"
              value={amount}
            />
          </label>

          <div className="v3-transaction-sheet__field">
            <span>{t('accounts.title')}</span>
            {isAccountLoading ? (
              <p className="v3-transaction-sheet__state">Loading...</p>
            ) : accounts.length === 0 ? (
              <p className="v3-transaction-sheet__state">
                {t('accounts.empty')}
              </p>
            ) : (
              <div className="v3-transaction-sheet__account-chips">
                {accounts.map((account) => (
                  <button
                    aria-pressed={accountId === account.id}
                    className={accountId === account.id ? 'is-active' : ''}
                    key={account.id}
                    onClick={() => setAccountId(account.id)}
                    type="button"
                  >
                    {account.name}
                  </button>
                ))}
              </div>
            )}
          </div>

          <label className="v3-transaction-sheet__field">
            <span>{t('transactions.date')}</span>
            <input
              onChange={(event) => setTransactionDate(event.target.value)}
              type="date"
              value={transactionDate}
            />
          </label>

          <label className="v3-transaction-sheet__field">
            <span>{t('transactions.title')}</span>
            <textarea
              onChange={(event) => setNote(event.target.value)}
              rows={3}
              value={note}
            />
          </label>

          <button
            className="v3-transaction-sheet__submit"
            disabled
            type="button"
          >
            TODO
          </button>
        </div>
      </section>
    </div>
  );
}
