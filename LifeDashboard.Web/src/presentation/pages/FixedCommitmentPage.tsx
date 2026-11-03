import { useEffect, useState } from 'react';
import { useFixedCommitmentStore } from '../../application/stores/fixedCommitmentStore';
import {
  CommitmentType,
  type IFixedCommitmentPayload,
} from '../../domain/FixedCommitment';

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
  const {
    commitments,
    isLoading,
    error,
    fetchCommitments,
    addCommitment,
    updateCommitment,
    removeCommitment,
  } = useFixedCommitmentStore();
  const [formData, setFormData] = useState<IFixedCommitmentPayload>(emptyForm);
  const [editingId, setEditingId] = useState<string | null>(null);

  useEffect(() => {
    void fetchCommitments().catch(() => undefined);
  }, [fetchCommitments]);

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
  };

  const handleDelete = async (id: string) => {
    try {
      await removeCommitment(id);
      if (editingId === id) {
        setFormData(emptyForm);
        setEditingId(null);
      }
    } catch {
      // The store exposes the API error in the page.
    }
  };

  return (
    <section>
      <h1>固定責任</h1>

      <form onSubmit={handleSubmit}>
        <h2>{editingId ? '編輯固定責任' : '新增固定責任'}</h2>
        <p>
          <label>
            名稱：
            <input
              type="text"
              value={formData.name}
              onChange={(event) => setFormData({ ...formData, name: event.target.value })}
              required
            />
          </label>
        </p>
        <p>
          <label>
            每月金額：
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
        </p>
        <p>
          <label>
            CommitmentType：
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
        </p>
        <p>
          <label>
            開始月份：
            <input
              type="month"
              value={formData.startMonth}
              onChange={(event) => setFormData({
                ...formData,
                startMonth: event.target.value,
              })}
              required
            />
          </label>
        </p>
        <p>
          <label>
            結束月份：
            <input
              type="month"
              value={formData.endMonth ?? ''}
              onChange={(event) => setFormData({
                ...formData,
                endMonth: event.target.value || null,
              })}
            />
          </label>
        </p>
        <button type="submit" disabled={isLoading}>
          {editingId ? '儲存修改' : '新增'}
        </button>
        {editingId && (
          <button
            type="button"
            onClick={() => {
              setFormData(emptyForm);
              setEditingId(null);
            }}
          >
            取消編輯
          </button>
        )}
      </form>

      {error && <p role="alert">{error}</p>}

      <h2>固定責任列表</h2>
      {isLoading && commitments.length === 0 ? (
        <p>載入中…</p>
      ) : commitments.length === 0 ? (
        <p>目前沒有固定責任。</p>
      ) : (
        <ul>
          {commitments.map((commitment) => (
            <li key={commitment.id}>
              <span>
                {commitment.name} — {commitment.monthlyAmount.toFixed(2)} —{' '}
                {getCommitmentTypeName(commitment.commitmentType)} —{' '}
                {toMonthInput(commitment.startMonth)} —{' '}
                {commitment.endMonth ? toMonthInput(commitment.endMonth) : '無結束月份'}
              </span>
              <button type="button" onClick={() => handleEdit(commitment.id)}>編輯</button>
              <button type="button" onClick={() => handleDelete(commitment.id)}>刪除</button>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
