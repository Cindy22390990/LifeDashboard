import axios from 'axios';
import { useEffect, useState } from 'react';
import { useDashboardSummaryStore } from '../../application/stores/dashboardSummaryStore';
import { useCategoryStore } from '../../application/stores/categoryStore';
import { useTransactionStore } from '../../application/stores/transactionStore';
import BudgetRadarPanel from '../components/BudgetRadarPanel';
import GoalPyramid from '../components/dashboard/GoalPyramid';
import { AccountType } from '../../domain/Account';
import { TransactionType } from '../../domain/Transaction';
import type { ICreateTransactionPayload } from '../../domain/Transaction';
import './DashboardPage.css';

interface ApiError {
  message?: string;
  Message?: string;
}

function getLocalDateString(date: Date): string {
  const year = String(date.getFullYear()).padStart(4, '0');
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');

  return `${year}-${month}-${day}`;
}

const currencyFormatter = new Intl.NumberFormat('zh-TW', {
  style: 'currency',
  currency: 'TWD',
  maximumFractionDigits: 0,
});

function formatCurrency(amount: number): string {
  return currencyFormatter.format(amount);
}

function getAccountTypeLabel(type: AccountType): string {
  switch (type) {
    case AccountType.Cashflow:
      return '日常現金流';
    case AccountType.Saving:
      return '儲備帳戶';
    case AccountType.Invest:
      return '投資帳戶';
  }
}

interface DashboardPageProps {
  onNavigateToCategories?: () => void;
}

export default function DashboardPage({ onNavigateToCategories }: DashboardPageProps) {
  const [currentDate] = useState(() => getLocalDateString(new Date()));
  const {
    summary,
    loading,
    error,
    fetchDashboardSummary,
  } = useDashboardSummaryStore();
  const radarState = summary?.budget ?? null;
  const accounts = summary?.accounts ?? [];
  const accountBalanceTotal = accounts.reduce(
    (total, account) => total + account.currentBalance,
    0,
  );
  const displayDate = new Date(`${currentDate}T00:00:00`).toLocaleDateString('zh-TW', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    weekday: 'long',
  });
  const {
    categories,
    isLoading: isCategoryLoading,
    error: categoryError,
    fetchCategories,
  } = useCategoryStore();
  const { isLoading: isTransactionLoading, addTransaction } = useTransactionStore();
  const [formData, setFormData] = useState<ICreateTransactionPayload>({
    accountId: '',
    amount: 0,
    type: TransactionType.Expense,
    description: '',
    transactionDate: currentDate,
  });
  const [submitError, setSubmitError] = useState<string | null>(null);

  useEffect(() => {
    void fetchDashboardSummary(currentDate).catch(() => undefined);
  }, [currentDate, fetchDashboardSummary]);

  useEffect(() => {
    void fetchCategories().catch(() => undefined);
  }, [fetchCategories]);

  const isTransfer = formData.type === TransactionType.Transfer;
  const hasSameTransferAccounts =
    isTransfer &&
    Boolean(formData.accountId) &&
    Boolean(formData.toAccountId) &&
    formData.accountId === formData.toAccountId;
  const canTransfer = accounts.length >= 2 && !hasSameTransferAccounts;

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setSubmitError(null);

    if (!formData.transactionDate || !formData.accountId || formData.amount <= 0) {
      setSubmitError('請填寫日期、帳戶及大於 0 的金額。');
      return;
    }

    if (formData.type === TransactionType.Expense && !formData.categoryId) {
      setSubmitError('支出交易必須選擇分類。');
      return;
    }

    if (isTransfer) {
      if (accounts.length < 2) {
        setSubmitError('轉帳需要至少兩個帳戶，請先新增第二個帳戶。');
        return;
      }
      if (!formData.toAccountId) {
        setSubmitError('請選擇轉入帳戶。');
        return;
      }
      if (hasSameTransferAccounts) {
        setSubmitError('來源帳戶與轉入帳戶不可相同。');
        return;
      }
    }

    const payload: ICreateTransactionPayload = { ...formData };
    if (isTransfer) {
      delete payload.categoryId;
    } else {
      delete payload.toAccountId;
      if (formData.type === TransactionType.Income && !payload.categoryId) {
        payload.categoryId = null;
      }
    }

    try {
      await addTransaction(payload);
      setFormData((previous) => ({
        ...previous,
        amount: 0,
        description: '',
        categoryId: undefined,
      }));
      await fetchDashboardSummary(currentDate).catch(() => undefined);
    } catch (requestError) {
      const message = axios.isAxiosError<ApiError>(requestError)
        ? requestError.response?.data?.message
          ?? requestError.response?.data?.Message
          ?? '新增交易失敗，請檢查資料後重試。'
        : '新增交易失敗，請檢查資料後重試。';
      setSubmitError(message);
    }
  };

  return (
    <div className="dashboard-page">
      <header className="dashboard-header">
        <div>
          <p className="dashboard-header__eyebrow">LIFE DASHBOARD</p>
          <h1>生活財務總覽</h1>
          <p className="dashboard-header__subtitle">目標、預算與帳戶狀態，一頁掌握。</p>
        </div>
        <time className="dashboard-header__date" dateTime={currentDate}>{displayDate}</time>
      </header>

      {loading && <p>載入中…</p>}
      {!loading && error && <p className="dashboard-feedback" role="alert">{error}</p>}

      {!loading && !error && summary && (
        <>
          <section className="dashboard-overview" aria-labelledby="dashboard-overview-title">
            <div className="dashboard-section-heading">
              <div>
                <p className="dashboard-section-heading__eyebrow">FINANCIAL SNAPSHOT</p>
                <h2 id="dashboard-overview-title">財務總覽</h2>
              </div>
              <span className="dashboard-overview__period">截至 {displayDate}</span>
            </div>
            <div className="dashboard-overview__metrics">
              <div className="dashboard-overview__balance">
                <span>帳戶餘額合計</span>
                <strong>{formatCurrency(accountBalanceTotal)}</strong>
              </div>
              <div className="dashboard-overview__account-count">
                <span>管理中的帳戶</span>
                <strong>{accounts.length}<small> 個</small></strong>
              </div>
            </div>
          </section>

          <section className="dashboard-primary" aria-label="人生目標">
            <GoalPyramid goals={summary.goals} />
          </section>

          {radarState && (
            <section className="dashboard-secondary" aria-labelledby="dashboard-budget-title">
              <div className="dashboard-section-heading">
                <div>
                  <p className="dashboard-section-heading__eyebrow">TODAY</p>
                  <h2 id="dashboard-budget-title">今日預算安全額度</h2>
                </div>
              </div>
              <BudgetRadarPanel radarState={radarState} />
            </section>
          )}

          <section className="dashboard-details" aria-labelledby="dashboard-accounts-title">
            <div className="dashboard-section-heading">
              <div>
                <p className="dashboard-section-heading__eyebrow">ACCOUNTS</p>
                <h2 id="dashboard-accounts-title">帳戶總覽</h2>
              </div>
              <span className="dashboard-details__count">{accounts.length} 個帳戶</span>
            </div>
            {accounts.length === 0 ? (
              <p className="dashboard-empty">目前沒有帳戶資料。</p>
            ) : (
              <ul className="dashboard-account-list">
                {accounts.map((account) => (
                  <li className="dashboard-account" key={account.id}>
                    <div className="dashboard-account__identity">
                      <h3>{account.name}</h3>
                      <span>{getAccountTypeLabel(account.type)}</span>
                    </div>
                    <strong>{formatCurrency(account.currentBalance)}</strong>
                  </li>
                ))}
              </ul>
            )}
          </section>
        </>
      )}

      <section className="dashboard-quick-add" aria-labelledby="dashboard-quick-add-title">
        <div className="dashboard-section-heading">
          <div>
            <p className="dashboard-section-heading__eyebrow">QUICK ENTRY</p>
            <h2 id="dashboard-quick-add-title">快速記帳</h2>
          </div>
        </div>
        <form onSubmit={handleSubmit}>
          <p>
            <label>
              日期：
              <input
                type="date"
                value={formData.transactionDate}
                onChange={(event) => setFormData({ ...formData, transactionDate: event.target.value })}
                required
              />
            </label>
          </p>

          <fieldset>
            <legend>交易類型</legend>
            <label>
              <input
                type="radio"
                name="transactionType"
                value={TransactionType.Expense}
                checked={formData.type === TransactionType.Expense}
                onChange={() => setFormData({
                  ...formData,
                  type: TransactionType.Expense,
                  toAccountId: undefined,
                })}
              />
              支出
            </label>
            <label>
              <input
                type="radio"
                name="transactionType"
                value={TransactionType.Income}
                checked={formData.type === TransactionType.Income}
                onChange={() => setFormData({
                  ...formData,
                  type: TransactionType.Income,
                  toAccountId: undefined,
                })}
              />
              收入
            </label>
            <label>
              <input
                type="radio"
                name="transactionType"
                value={TransactionType.Transfer}
                checked={isTransfer}
                onChange={() => setFormData({
                  ...formData,
                  type: TransactionType.Transfer,
                  categoryId: undefined,
                })}
              />
              轉帳
            </label>
          </fieldset>

          <p>
            <label>
              {isTransfer ? '來源帳戶：' : '帳戶：'}
              <select
                value={formData.accountId}
                onChange={(event) => setFormData({
                  ...formData,
                  accountId: event.target.value,
                })}
                required
              >
                <option value="">請選擇帳戶</option>
                {accounts.map((account) => (
                  <option key={account.id} value={account.id}>{account.name}</option>
                ))}
              </select>
            </label>
          </p>

          {isTransfer && (
            <p>
              <label>
                轉入帳戶：
                <select
                  value={formData.toAccountId ?? ''}
                  onChange={(event) => setFormData({
                    ...formData,
                    toAccountId: event.target.value || undefined,
                  })}
                  required
                  disabled={accounts.length < 2}
                >
                  <option value="">請選擇轉入帳戶</option>
                  {accounts.map((account) => (
                    <option
                      key={account.id}
                      value={account.id}
                      disabled={account.id === formData.accountId}
                    >
                      {account.name}
                    </option>
                  ))}
                </select>
              </label>
            </p>
          )}

          {isTransfer && accounts.length < 2 && (
            <p role="alert">轉帳需要至少兩個帳戶，請先新增第二個帳戶。</p>
          )}
          {hasSameTransferAccounts && (
            <p role="alert">來源帳戶與轉入帳戶不可相同。</p>
          )}

          <p>
            <label>
              金額：
              <input
                type="number"
                min="0.01"
                step="0.01"
                value={formData.amount || ''}
                onChange={(event) => setFormData({
                  ...formData,
                  amount: Number(event.target.value),
                })}
                required
              />
            </label>
          </p>

          {!isTransfer && (
            <p>
              <label>
                分類：
                <select
                  value={formData.categoryId ?? ''}
                  required={formData.type === TransactionType.Expense}
                  onChange={(event) => setFormData({
                    ...formData,
                    categoryId: event.target.value || null,
                  })}
                >
                  {formData.type === TransactionType.Expense ? (
                    <option value="" disabled>請選擇分類</option>
                  ) : (
                    <option value="">未分類（選填）</option>
                  )}
                  {categories.map((category) => (
                    <option key={category.id} value={category.id}>
                      {category.icon} {category.name}
                    </option>
                  ))}
                </select>
              </label>
            </p>
          )}
          {!isTransfer && !isCategoryLoading && categories.length === 0 && (
            <p>
              尚未建立分類
              {onNavigateToCategories && (
                <button type="button" onClick={onNavigateToCategories}>前往分類管理</button>
              )}
            </p>
          )}
          {isCategoryLoading && !isTransfer && <p>載入分類中…</p>}
          {categoryError && !isTransfer && <p role="alert">{categoryError}</p>}

          <p>
            <label>
              備註：
              <input
                type="text"
                value={formData.description}
                onChange={(event) => setFormData({
                  ...formData,
                  description: event.target.value,
                })}
              />
            </label>
          </p>

          {submitError && <p role="alert">{submitError}</p>}
          <button
            type="submit"
            disabled={
              isTransactionLoading ||
              accounts.length === 0 ||
              (formData.type === TransactionType.Expense && categories.length === 0) ||
              (isTransfer && !canTransfer)
            }
          >
            {isTransactionLoading ? '儲存中…' : '新增交易'}
          </button>
        </form>
      </section>
    </div>
  );
}
