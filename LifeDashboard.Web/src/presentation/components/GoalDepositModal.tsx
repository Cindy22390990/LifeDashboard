import { useState, type FormEvent } from 'react';
import type { IAccount } from '../../domain/Account';
import type { IGoal, IGoalDepositRequest } from '../../domain/Goal';

interface GoalDepositModalProps {
  goal: IGoal;
  accounts: IAccount[];
  isSubmitting: boolean;
  onClose: () => void;
  onSubmit: (request: IGoalDepositRequest) => Promise<void>;
}

export default function GoalDepositModal({
  goal,
  accounts,
  isSubmitting,
  onClose,
  onSubmit,
}: GoalDepositModalProps) {
  const [amount, setAmount] = useState('');
  const [fromAccountId, setFromAccountId] = useState('');
  const [toAccountId, setToAccountId] = useState('');
  const [note, setNote] = useState('');
  const [formError, setFormError] = useState<string | null>(null);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (fromAccountId === toAccountId) {
      setFormError('來源帳戶與轉入帳戶不可相同。');
      return;
    }

    setFormError(null);
    await onSubmit({
      amount: Number(amount),
      fromAccountId,
      toAccountId,
      note,
    });
  };

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="goal-deposit-title"
      style={{
        position: 'fixed',
        inset: 0,
        zIndex: 1000,
        display: 'grid',
        placeItems: 'center',
        padding: '16px',
        backgroundColor: 'rgba(0, 0, 0, 0.45)',
      }}
    >
      <section
        style={{
          width: 'min(100%, 480px)',
          padding: '24px',
          borderRadius: '12px',
          backgroundColor: 'white',
          boxShadow: '0 12px 40px rgba(0, 0, 0, 0.2)',
        }}
      >
        <h2 id="goal-deposit-title">存入目標：{goal.name}</h2>
        <form onSubmit={handleSubmit}>
          <p>
            <label htmlFor="goal-deposit-amount">金額</label>
            <input
              id="goal-deposit-amount"
              type="number"
              min="0.01"
              step="0.01"
              required
              value={amount}
              onChange={(event) => setAmount(event.target.value)}
              style={{ display: 'block', width: '100%', boxSizing: 'border-box' }}
            />
          </p>
          <p>
            <label htmlFor="goal-deposit-from-account">來源帳戶</label>
            <select
              id="goal-deposit-from-account"
              required
              value={fromAccountId}
              onChange={(event) => setFromAccountId(event.target.value)}
              style={{ display: 'block', width: '100%' }}
            >
              <option value="">選擇來源帳戶</option>
              {accounts.map((account) => (
                <option key={account.id} value={account.id}>
                  {account.name} — $ {account.currentBalance.toLocaleString()}
                </option>
              ))}
            </select>
          </p>
          <p>
            <label htmlFor="goal-deposit-to-account">轉入帳戶</label>
            <select
              id="goal-deposit-to-account"
              required
              value={toAccountId}
              onChange={(event) => setToAccountId(event.target.value)}
              style={{ display: 'block', width: '100%' }}
            >
              <option value="">選擇轉入帳戶</option>
              {accounts.map((account) => (
                <option key={account.id} value={account.id}>
                  {account.name} — $ {account.currentBalance.toLocaleString()}
                </option>
              ))}
            </select>
          </p>
          <p>
            <label htmlFor="goal-deposit-note">備註</label>
            <textarea
              id="goal-deposit-note"
              value={note}
              onChange={(event) => setNote(event.target.value)}
              rows={3}
              style={{ display: 'block', width: '100%', boxSizing: 'border-box' }}
            />
          </p>
          {accounts.length < 2 && (
            <p role="alert">至少需要兩個帳戶才能進行存款轉帳。</p>
          )}
          {formError && <p role="alert">{formError}</p>}
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px' }}>
            <button type="button" onClick={onClose} disabled={isSubmitting}>
              取消
            </button>
            <button
              type="submit"
              disabled={isSubmitting || accounts.length < 2}
            >
              {isSubmitting ? '存款中…' : '確認存款'}
            </button>
          </div>
        </form>
      </section>
    </div>
  );
}
