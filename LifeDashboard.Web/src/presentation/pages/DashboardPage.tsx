import axios from 'axios';
import { useEffect, useState } from 'react';
import { useDashboardSummaryStore } from '../../application/stores/dashboardSummaryStore';
import { useCategoryStore } from '../../application/stores/categoryStore';
import { useTransactionStore } from '../../application/stores/transactionStore';
import BudgetRadarPanel from '../components/BudgetRadarPanel';
import GoalPyramid from '../components/dashboard/GoalPyramid';
import { TransactionType } from '../../domain/Transaction';
import type { ICreateTransactionPayload } from '../../domain/Transaction';

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
    <section>
      <h1>生活閒錢雷達</h1>

      {loading && <p>載入中…</p>}
      {!loading && error && <p role="alert">{error}</p>}

      {!loading && !error && radarState && (
        <BudgetRadarPanel radarState={radarState} />
      )}
      {!loading && !error && summary && (
        <GoalPyramid goals={summary.goals} />
      )}

      <section>
        <h2>Quick Add</h2>
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
    </section>
  );
}
