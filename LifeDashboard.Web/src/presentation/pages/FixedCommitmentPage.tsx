import { useEffect, useRef, useState } from 'react';
import { useOutletContext } from 'react-router-dom';
import { useFixedCommitmentStore } from '../../application/stores/fixedCommitmentStore';
import { useSettingStore } from '../../application/stores/settingStore';
import {
  CommitmentType,
  type IFixedCommitmentPayload,
} from '../../domain/FixedCommitment';
import './FixedCommitmentPage.css';

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
  const { setting, isLoading: isSettingLoading, error: settingError, fetchSetting } = useSettingStore();
  const [formData, setFormData] = useState<IFixedCommitmentPayload>(emptyForm);
  const [editingId, setEditingId] = useState<string | null>(null);
  const commitmentFormRef = useRef<HTMLDetailsElement>(null);

  useEffect(() => {
    void fetchCommitments().catch(() => undefined);
  }, [fetchCommitments]);

  useEffect(() => {
    void fetchSetting();
  }, [fetchSetting]);

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
    { key: 'living', label: 'Living', description: '生活', value: setting?.living },
    { key: 'savings', label: 'Savings', description: '儲蓄', value: setting?.savings },
    { key: 'investment', label: 'Investment', description: '投資', value: setting?.investment },
    { key: 'entertainment', label: 'Entertainment', description: '娛樂', value: setting?.entertainment },
  ];

  return (
    <div className="responsibilities-page">
      <header className="responsibilities-header">
        <p className="responsibilities-eyebrow">MONTHLY PLAN</p>
        <h1>Monthly Allocation</h1>
        <p className="responsibilities-subtitle">檢視每月收入、固定責任與資產分配。</p>
      </header>

      <section className="responsibilities-section income-section" aria-labelledby="monthly-income-title">
        <div className="responsibilities-section-heading">
          <div>
            <p className="responsibilities-eyebrow">01 / INCOME</p>
            <h2 id="monthly-income-title">Monthly Income</h2>
          </div>
        </div>
        <div className="income-summary surface-card">
          <div>
            <span className="summary-label">每月收入</span>
            <strong className="income-value">—</strong>
            <span className="summary-note">目前沒有可用的每月收入資料</span>
          </div>
          <button type="button" className="secondary-action" disabled aria-label="每月收入編輯功能尚未提供">
            編輯
          </button>
        </div>
      </section>

      <section className="responsibilities-section" aria-labelledby="fixed-responsibilities-title">
        <div className="responsibilities-section-heading">
          <div>
            <p className="responsibilities-eyebrow">02 / COMMITMENTS</p>
            <h2 id="fixed-responsibilities-title">Fixed Responsibilities</h2>
          </div>
          <span className="section-count">{commitments.length} 項</span>
        </div>

        {error && <p className="page-error" role="alert">{error}</p>}
        {isLoading && commitments.length === 0 ? (
          <p className="empty-message">載入中…</p>
        ) : commitments.length === 0 ? (
          <p className="empty-message">目前沒有固定責任。</p>
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
          <span>Total Fixed Responsibilities</span>
          <strong>—</strong>
          <small>合計資料目前未提供</small>
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
            <h2 id="allocation-base-title">Available Allocation Base</h2>
          </div>
        </div>
        <div className="allocation-equation surface-card" aria-label="可分配基數摘要，計算資料尚未提供">
          <div className="equation-item">
            <span>每月收入</span>
            <strong>尚未提供</strong>
          </div>
          <span className="equation-operator" aria-hidden="true">−</span>
          <div className="equation-item">
            <span>固定責任合計</span>
            <strong>尚未計算</strong>
          </div>
          <span className="equation-operator" aria-hidden="true">=</span>
          <div className="equation-item equation-result">
            <span>Available Allocation Base</span>
            <strong>—</strong>
          </div>
        </div>
      </section>

      <section className="responsibilities-section" aria-labelledby="allocation-ratio-title">
        <div className="responsibilities-section-heading">
          <div>
            <p className="responsibilities-eyebrow">04 / ALLOCATION RATIO</p>
            <h2 id="allocation-ratio-title">Allocation Ratio</h2>
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
                <span>{ratio.description}</span>
              </div>
            ))}
          </dl>
        )}
      </section>

      <section className="responsibilities-section" aria-labelledby="allocation-preview-title">
        <div className="responsibilities-section-heading">
          <div>
            <p className="responsibilities-eyebrow">05 / PREVIEW</p>
            <h2 id="allocation-preview-title">Allocation Preview</h2>
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
          <h2 id="account-management-title">Account Management</h2>
        </div>
        <button type="button" className="secondary-action" onClick={openAccountManagement}>
          Manage Accounts
        </button>
      </section>
    </div>
  );
}
