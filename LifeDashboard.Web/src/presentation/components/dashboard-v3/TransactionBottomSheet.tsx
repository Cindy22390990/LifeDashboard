import { useEffect, useRef, useState, type FormEvent } from 'react';
import axios from 'axios';
import { useTranslation } from 'react-i18next';
import { useAccountStore } from '../../../application/stores/accountStore';
import { useTransactionStore } from '../../../application/stores/transactionStore';
import {
  TransactionType,
  type ICreateTransactionPayload,
  type TransactionType as TransactionTypeValue,
} from '../../../domain/Transaction';
import './TransactionBottomSheet.css';

interface TransactionBottomSheetProps {
  isOpen: boolean;
  defaultDate: string;
  onClose: () => void;
  onSuccess?: (payload: ICreateTransactionPayload) => void | Promise<void>;
}

function isValidDateOnly(value: string): boolean {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  if (!match) return false;

  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  if (month < 1 || month > 12 || day < 1) return false;

  const leapYear = year % 4 === 0 && (year % 100 !== 0 || year % 400 === 0);
  const daysInMonth = [
    31,
    leapYear ? 29 : 28,
    31,
    30,
    31,
    30,
    31,
    31,
    30,
    31,
    30,
    31,
  ];

  return day <= daysInMonth[month - 1];
}

export default function TransactionBottomSheet({
  isOpen,
  defaultDate,
  onClose,
  onSuccess,
}: TransactionBottomSheetProps) {
  const { t } = useTranslation();
  const accounts = useAccountStore((state) => state.accounts);
  const isAccountLoading = useAccountStore((state) => state.isLoading);
  const fetchAccounts = useAccountStore((state) => state.fetchAccounts);
  const addTransaction = useTransactionStore((state) => state.addTransaction);
  const [transactionType, setTransactionType] = useState<TransactionTypeValue>(
    TransactionType.Expense,
  );
  const [amount, setAmount] = useState('');
  const [accountId, setAccountId] = useState('');
  const [transactionDate, setTransactionDate] = useState(defaultDate);
  const [note, setNote] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [amountError, setAmountError] = useState('');
  const [accountError, setAccountError] = useState('');
  const [dateError, setDateError] = useState('');
  const [submitError, setSubmitError] = useState('');
  const hasRequestedAccounts = useRef(false);
  const accountCount = accounts.length;
  const firstAccountId = accounts[0]?.id ?? '';
  const hasSelectedAccount = accountId
    ? accounts.some((account) => account.id === accountId)
    : false;

  useEffect(() => {
    if (!isOpen) return;

    setTransactionDate(defaultDate);
    setAmount('');
    setNote('');
    setTransactionType(TransactionType.Expense);
    setAmountError('');
    setAccountError('');
    setDateError('');
    setSubmitError('');
  }, [isOpen, defaultDate]);

  useEffect(() => {
    if (!isOpen) return;

    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && !isSubmitting) {
        onClose();
      }
    };

    document.addEventListener('keydown', handleEscape);
    return () => document.removeEventListener('keydown', handleEscape);
  }, [isOpen, isSubmitting, onClose]);

  useEffect(() => {
    if (!isOpen) {
      hasRequestedAccounts.current = false;
      return;
    }

    if (
      accountCount === 0 &&
      !isAccountLoading &&
      !hasRequestedAccounts.current
    ) {
      hasRequestedAccounts.current = true;
      void fetchAccounts();
    }
  }, [isOpen, accountCount, isAccountLoading, fetchAccounts]);

  useEffect(() => {
    if (hasSelectedAccount) return;
    setAccountId(firstAccountId);
  }, [firstAccountId, hasSelectedAccount]);

  const getApiErrorMessage = (error: unknown): string => {
    if (axios.isAxiosError<{ message?: string; Message?: string }>(error)) {
      return error.response?.data?.message
        ?? error.response?.data?.Message
        ?? '無法新增交易，請稍後再試。';
    }

    return error instanceof Error
      ? error.message
      : '無法新增交易，請稍後再試。';
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setAmountError('');
    setAccountError('');
    setDateError('');
    setSubmitError('');

    const numericAmount = Number(amount);
    if (amount.trim() === '' || !Number.isFinite(numericAmount) || numericAmount <= 0) {
      setAmountError('金額必須大於 0');
      return;
    }

    if (!accountId || !accounts.some((account) => account.id === accountId)) {
      setAccountError('請選擇有效帳戶');
      return;
    }

    if (!isValidDateOnly(transactionDate)) {
      setDateError('請選擇有效日期');
      return;
    }

    const payload: ICreateTransactionPayload = {
      accountId,
      amount: numericAmount,
      type: transactionType,
      transactionDate,
      description: note.trim(),
    };

    setIsSubmitting(true);
    try {
      await addTransaction(payload);
      await onSuccess?.(payload);
      onClose();
    } catch (error) {
      setSubmitError(getApiErrorMessage(error));
    } finally {
      setIsSubmitting(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div
      className="v3-transaction-sheet-backdrop"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget && !isSubmitting) {
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
            disabled={isSubmitting}
            type="button"
          >
            ×
          </button>
        </div>
        <form className="v3-transaction-sheet__form" onSubmit={handleSubmit}>
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
              onChange={(event) => {
                setAmount(event.target.value);
                setAmountError('');
                setSubmitError('');
              }}
              placeholder="0"
              type="number"
              value={amount}
            />
            {amountError && (
              <p className="v3-transaction-sheet__error" role="alert">
                {amountError}
              </p>
            )}
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
                    onClick={() => {
                      setAccountId(account.id);
                      setAccountError('');
                      setSubmitError('');
                    }}
                    type="button"
                  >
                    {account.name}
                  </button>
                ))}
              </div>
            )}
            {accountError && (
              <p className="v3-transaction-sheet__error" role="alert">
                {accountError}
              </p>
            )}
          </div>

          <label className="v3-transaction-sheet__field">
            <span>{t('transactions.date')}</span>
            <input
              onChange={(event) => {
                setTransactionDate(event.target.value);
                setDateError('');
                setSubmitError('');
              }}
              type="date"
              value={transactionDate}
            />
            {dateError && (
              <p className="v3-transaction-sheet__error" role="alert">
                {dateError}
              </p>
            )}
          </label>

          <label className="v3-transaction-sheet__field">
            <span>{t('transactions.title')}</span>
            <textarea
              onChange={(event) => {
                setNote(event.target.value);
                setSubmitError('');
              }}
              rows={3}
              value={note}
            />
          </label>

          {submitError && (
            <p className="v3-transaction-sheet__error" role="alert">
              {submitError}
            </p>
          )}

          <button
            className="v3-transaction-sheet__submit"
            disabled={isSubmitting || accounts.length === 0}
            type="submit"
          >
            {isSubmitting ? 'Saving...' : 'Save transaction'}
          </button>
        </form>
      </section>
    </div>
  );
}
