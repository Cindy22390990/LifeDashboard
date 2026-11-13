import axios from 'axios';
import { useEffect, useRef, useState } from 'react';
import { useOutletContext } from 'react-router-dom';
import { useAccountStore } from '../../application/stores/accountStore';
import { useFixedCommitmentStore } from '../../application/stores/fixedCommitmentStore';
import { useSettingStore } from '../../application/stores/settingStore';
import { useTransactionStore } from '../../application/stores/transactionStore';
import {
  CommitmentType,
  type IFixedCommitmentPayload,
} from '../../domain/FixedCommitment';
import { TransactionType, type ICreateTransactionPayload } from '../../domain/Transaction';
import './FixedCommitmentPage.css';

interface ApiError {
  message?: string;
  Message?: string;
}

interface ResponsibilitiesPageActions {
  openAccountManagement: () => void;
  openSystemSettings: () => void;
}

const emptyForm: IFixedCommitmentPayload = {
  name: '',
  monthlyAmount: 0,
  commitmentType: CommitmentType.Rent,
  startMonth: '',
  endMonth: null,
};

function toMonthInput(date: string): string {
  return date.slice(0, 7);
}

function toApiDate(month: string): string {
  return month ? `${month}-01` : '';
}

function getLocalDateString(date: Date): string {
  const year = String(date.getFullYear()).padStart(4, '0');
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function getIncomeErrorMessage(error: unknown): string {
  if (axios.isAxiosError<ApiError>(error)) {
    return error.response?.data?.message
      ?? error.response?.data?.Message
      ?? '無法建立薪資收入，請稍後再試。';
  }

  return '建立薪資收入時發生未預期錯誤。';
}

function getCommitmentTypeName(type: CommitmentType): string {
  switch (type) {
    case CommitmentType.Rent:
      return '租金';
    case CommitmentType.Insurance:
      return '保險';
    case CommitmentType.Installment:
      return '分期';
    case CommitmentType.FamilySupport:
      return '家庭支援';
  }
}

export default function FixedCommitmentPage() {
  const { openAccountManagement, openSystemSettings } =
    useOutletContext<ResponsibilitiesPageActions>();
  const {
    commitments,
    isLoading,
    error,
    fetchCommitments,
    addCommitment,
    updateCommitment,
    removeCommitment,
  } = useFixedCommitmentStore();
  const {
    accounts,
    isLoading: isAccountsLoading,
    fetchAccounts,
  } = useAccountStore();
  const { addTransaction } = useTransactionStore();
  const { setting, isLoading: isSettingLoading, error: settingError, fetchSetting } = useSettingStore();
  const [monthlyIncome, setMonthlyIncome] = useState('');
  const [selectedIncomeAccountId, setSelectedIncomeAccountId] = useState('');
  const [incomeTransactionDate, setIncomeTransactionDate] = useState(
    () => getLocalDateString(new Date()),
  );
  const [isCreatingIncome, setIsCreatingIncome] = useState(false);
  const [incomeError, setIncomeError] = useState<string | null>(null);
  const [incomeSuccess, setIncomeSuccess] = useState<string | null>(null);
  const [formData, setFormData] = useState<IFixedCommitmentPayload>(emptyForm);
  const [editingId, setEditingId] = useState<string | null>(null);
  const commitmentFormRef = useRef<HTMLDetailsElement>(null);

  useEffect(() => {
    void fetchCommitments().catch(() => undefined);
  }, [fetchCommitments]);

  useEffect(() => {
    void fetchSetting();
  }, [fetchSetting]);

  useEffect(() => {
    void fetchAccounts();
  }, [fetchAccounts]);

  const handleCreateIncomeTransaction = async (event: React.FormEvent) => {
    event.preventDefault();
    setIncomeError(null);
    setIncomeSuccess(null);

    const amount = Number(monthlyIncome);
    if (!monthlyIncome.trim() || !Number.isFinite(amount) || amount <= 0) {
      setIncomeError('請輸入大於 0 的薪資金額。');
      return;
    }

    if (
      !selectedIncomeAccountId ||
      !accounts.some((account) => account.id === selectedIncomeAccountId)
    ) {
      setIncomeError('請選擇有效的入帳帳戶。');
      return;
    }

    if (!incomeTransactionDate) {
      setIncomeError('請選擇實際入帳日期。');
      return;
    }

    const payload: ICreateTransactionPayload = {
      accountId: selectedIncomeAccountId,
      amount,
      type: TransactionType.Income,
      transactionDate: incomeTransactionDate,
      description: '',
      categoryId: null,
    };

    setIsCreatingIncome(true);
    try {
      await addTransaction(payload);
      setMonthlyIncome('');
      setIncomeSuccess('薪資收入已建立，所選帳戶餘額已更新。');
      await fetchAccounts();
    } catch (requestError) {
      setIncomeError(getIncomeErrorMessage(requestError));
    } finally {
      setIsCreatingIncome(false);
    }
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    const payload: IFixedCommitmentPayload = {
      ...formData,
      startMonth: toApiDate(formData.startMonth),
      endMonth: formData.endMonth ? toApiDate(formData.endMonth) : null,
    };

    try {
      if (editingId) {
        await updateCommitment(editingId, payload);
      } else {
        await addCommitment(payload);
      }
      setFormData(emptyForm);
      setEditingId(null);
      commitmentFormRef.current?.removeAttribute('open');
    } catch {
      // The store exposes the API error in the page.
    }
  };

  const handleEdit = (id: string) => {
    const commitment = commitments.find((item) => item.id === id);
    if (!commitment) {
      return;
    }

    setFormData({
      name: commitment.name,
      monthlyAmount: commitment.monthlyAmount,
      commitmentType: commitment.commitmentType,
      startMonth: toMonthInput(commitment.startMonth),
      endMonth: commitment.endMonth ? toMonthInput(commitment.endMonth) : null,
    });
    setEditingId(commitment.id);
    if (commitmentFormRef.current) {
      commitmentFormRef.current.open = true;
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await removeCommitment(id);
      if (editingId === id) {
        setFormData(emptyForm);
        setEditingId(null);
        commitmentFormRef.current?.removeAttribute('open');
      }
    } catch {
      // The store exposes the API error in the page.
    }
  };

  const allocationRatios = [
    { key: 'living', label: '生活', description: '生活', value: setting?.living },
    { key: 'savings', label: '儲蓄', description: '儲蓄', value: setting?.savings },
    { key: 'investment', label: '投資', description: '投資', value: setting?.investment },
    { key: 'entertainment', label: '娛樂', description: '娛樂', value: setting?.entertainment },
  ];

  return (
    <div className="responsibilities-page">
      <header className="responsibilities-header">
        <p className="responsibilities-eyebrow">MONTHLY PLAN</p>
        <h1>本月財務規劃</h1>
        <p className="responsibilities-subtitle">先記錄本月實際收入，再整理固定責任與後續分配。</p>
      </header>

      <section className="responsibilities-section income-section" aria-labelledby="monthly-income-title">
        <div className="responsibilities-section-heading">
          <div>
            <p className="responsibilities-eyebrow">01 / INCOME</p>
            <h2 id="monthly-income-title">本月收入</h2>
          </div>
        </div>
        <div className="income-summary surface-card">
          <div className="income-entry-copy">
            <label className="summary-label" htmlFor="monthly-income-input">本月實際收到的薪資</label>
            <p className="summary-note">
              送出後會建立收入交易並增加所選帳戶餘額；預算雷達會依薪資週期納入此收入。
            </p>
          </div>
          <form
            className="income-form"
            onSubmit={(event) => { void handleCreateIncomeTransaction(event); }}
            noValidate
          >
            <label className="income-input-wrap" htmlFor="monthly-income-input">
              薪資金額
              <input
                id="monthly-income-input"
                type="number"
                min="0.01"
                step="0.01"
                inputMode="decimal"
                value={monthlyIncome}
                onChange={(event) => setMonthlyIncome(event.target.value)}
                placeholder="輸入本月實收金額"
                aria-describedby="monthly-income-note"
                required
              />
            </label>
            <div className="income-form-fields">
              <label className="income-field" htmlFor="income-account-select">
                入帳帳戶
                <select
                  id="income-account-select"
                  value={selectedIncomeAccountId}
                  onChange={(event) => setSelectedIncomeAccountId(event.target.value)}
                  disabled={isAccountsLoading || accounts.length === 0}
                  required
                >
                  <option value="">
                    {isAccountsLoading ? '載入帳戶中…' : '請選擇帳戶'}
                  </option>
                  {accounts.map((account) => (
                    <option key={account.id} value={account.id}>{account.name}</option>
                  ))}
                </select>
              </label>
              <label className="income-field" htmlFor="income-transaction-date">
                入帳日期
                <input
                  id="income-transaction-date"
                  type="date"
                  value={incomeTransactionDate}
                  onChange={(event) => setIncomeTransactionDate(event.target.value)}
                  required
                />
              </label>
            </div>
            {!isAccountsLoading && accounts.length === 0 && (
              <p className="income-helper" role="status">
                尚未建立帳戶，請先管理帳戶後再記錄薪資。
                <button
                  type="button"
                  className="text-action"
                  onClick={openAccountManagement}
                >
                  管理帳戶
                </button>
              </p>
            )}
            {incomeError && <p className="page-error" role="alert">{incomeError}</p>}
            {incomeSuccess && <p className="income-success" role="status">{incomeSuccess}</p>}
            <button
              type="submit"
              className="primary-action income-submit"
              disabled={isCreatingIncome || isAccountsLoading || accounts.length === 0}
            >
              {isCreatingIncome ? '建立中…' : '記錄薪資收入'}
            </button>
          </form>
        </div>
        <p className="income-helper" id="monthly-income-note">
          請填寫實際入帳的收入與日期；薪資週期依系統設定的發薪日計算。
        </p>
      </section>

      <section className="responsibilities-section" aria-labelledby="fixed-responsibilities-title">
        <div className="responsibilities-section-heading">
          <div>
            <p className="responsibilities-eyebrow">02 / COMMITMENTS</p>
            <h2 id="fixed-responsibilities-title">固定責任</h2>
          </div>
          <span className="section-count">{commitments.length} 項</span>
        </div>

        {error && <p className="page-error" role="alert">{error}</p>}
        {isLoading && commitments.length === 0 ? (
          <p className="empty-message responsibility-empty">正在載入固定責任…</p>
        ) : commitments.length === 0 ? (
          <div className="empty-message responsibility-empty">
            <strong>還沒有固定責任</strong>
            <span>新增每月固定支出，方便在規劃時一併檢視。</span>
          </div>
        ) : (
          <ul className="responsibility-list">
            {commitments.map((commitment) => (
              <li className="responsibility-row" key={commitment.id}>
                <div className="responsibility-identity">
                  <h3>{commitment.name}</h3>
                  <span>
                    {getCommitmentTypeName(commitment.commitmentType)} · {toMonthInput(commitment.startMonth)}
                    {commitment.endMonth ? ` – ${toMonthInput(commitment.endMonth)}` : ' 起，無結束月份'}
                  </span>
                </div>
                <strong className="responsibility-amount">{commitment.monthlyAmount.toFixed(2)}</strong>
                <div className="row-actions">
                  <button type="button" className="text-action" onClick={() => handleEdit(commitment.id)}>編輯</button>
                  <button type="button" className="text-action" onClick={() => handleDelete(commitment.id)}>刪除</button>
                </div>
              </li>
            ))}
          </ul>
        )}

        <div className="responsibility-total">
          <span>固定責任合計</span>
          <strong>—</strong>
          <small>合計金額尚未計算</small>
        </div>

        <details className="commitment-editor" ref={commitmentFormRef}>
          <summary>{editingId ? '編輯固定責任' : '新增固定責任'}</summary>
          <form onSubmit={handleSubmit} aria-labelledby="commitment-editor-title">
            <h3 id="commitment-editor-title">{editingId ? '編輯固定責任' : '新增固定責任'}</h3>
            <label>
              名稱
              <input
                type="text"
                value={formData.name}
                onChange={(event) => setFormData({ ...formData, name: event.target.value })}
                required
              />
            </label>
            <label>
              每月金額
              <input
                type="number"
                min="0.01"
                step="0.01"
                value={formData.monthlyAmount || ''}
                onChange={(event) => setFormData({
                  ...formData,
                  monthlyAmount: Number(event.target.value),
                })}
                required
              />
            </label>
            <label>
              責任類型
              <select
                value={formData.commitmentType}
                onChange={(event) => setFormData({
                  ...formData,
                  commitmentType: Number(event.target.value) as CommitmentType,
                })}
              >
                <option value={CommitmentType.Rent}>租金</option>
                <option value={CommitmentType.Insurance}>保險</option>
                <option value={CommitmentType.Installment}>分期</option>
                <option value={CommitmentType.FamilySupport}>家庭支援</option>
              </select>
            </label>
            <label>
              開始月份
              <input
                type="month"
                value={formData.startMonth}
                onChange={(event) => setFormData({ ...formData, startMonth: event.target.value })}
                required
              />
            </label>
            <label>
              結束月份
              <input
                type="month"
                value={formData.endMonth ?? ''}
                onChange={(event) => setFormData({ ...formData, endMonth: event.target.value || null })}
              />
            </label>
            <div className="editor-actions">
              <button type="submit" className="primary-action" disabled={isLoading}>
                {editingId ? '儲存修改' : '新增固定責任'}
              </button>
              {editingId && (
                <button
                  type="button"
                  className="secondary-action"
                  onClick={() => {
                    setFormData(emptyForm);
                    setEditingId(null);
                    if (commitmentFormRef.current) commitmentFormRef.current.open = false;
                  }}
                >
                  取消編輯
                </button>
              )}
            </div>
          </form>
        </details>
      </section>

      <section className="responsibilities-section" aria-labelledby="allocation-base-title">
        <div className="responsibilities-section-heading">
          <div>
            <p className="responsibilities-eyebrow">03 / AVAILABLE BASE</p>
            <h2 id="allocation-base-title">可分配閒錢</h2>
          </div>
        </div>
        <div className="allocation-equation surface-card" aria-label="可分配閒錢計算預覽，目前尚未計算">
          <div className="equation-item">
            <span>本月收入</span>
            <strong>尚未計算</strong>
          </div>
          <span className="equation-operator" aria-hidden="true">−</span>
          <div className="equation-item">
            <span>固定責任</span>
            <strong>尚未計算</strong>
          </div>
          <span className="equation-operator" aria-hidden="true">=</span>
          <div className="equation-item equation-result">
            <span>可分配閒錢</span>
            <strong>尚未計算</strong>
          </div>
        </div>
        <p className="allocation-note">未來將以本月實收收入扣除固定責任後，作為分配規劃的基礎。</p>
      </section>

      <section className="responsibilities-section" aria-labelledby="allocation-ratio-title">
        <div className="responsibilities-section-heading">
          <div>
            <p className="responsibilities-eyebrow">04 / ALLOCATION RATIO</p>
            <h2 id="allocation-ratio-title">分配比例</h2>
          </div>
          <button type="button" className="text-action" onClick={openSystemSettings}>
            編輯分配比例
          </button>
        </div>
        {settingError && <p className="page-error" role="alert">{settingError}</p>}
        {isSettingLoading && !setting ? (
          <p className="empty-message">載入分配比例中…</p>
        ) : (
          <dl className="ratio-grid">
            {allocationRatios.map((ratio) => (
              <div className="ratio-card surface-card" key={ratio.key}>
                <dt>{ratio.label}</dt>
                <dd>{ratio.value === undefined ? '—' : `${ratio.value}%`}</dd>
              </div>
            ))}
          </dl>
        )}
      </section>

      <section className="responsibilities-section" aria-labelledby="allocation-preview-title">
        <div className="responsibilities-section-heading">
          <div>
            <p className="responsibilities-eyebrow">05 / PREVIEW</p>
            <h2 id="allocation-preview-title">分配預覽</h2>
          </div>
        </div>
        <div className="preview-grid">
          {allocationRatios.map((ratio) => (
            <article className="preview-card surface-card" key={ratio.key}>
              <span>{ratio.description}</span>
              <strong>—</strong>
              <small>可分配金額尚未計算</small>
            </article>
          ))}
        </div>
      </section>

      <section className="account-management" aria-labelledby="account-management-title">
        <div>
          <p className="responsibilities-eyebrow">MANAGEMENT</p>
          <h2 id="account-management-title">帳戶管理</h2>
        </div>
        <button type="button" className="secondary-action" onClick={openAccountManagement}>
          管理帳戶
        </button>
      </section>
    </div>
  );
}
