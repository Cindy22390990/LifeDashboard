import React, { useEffect, useState } from 'react';
import { useTransactionStore } from '../../application/stores/transactionStore';
import { useAccountStore } from '../../application/stores/accountStore';
import { useCategoryStore } from '../../application/stores/categoryStore';
import { TransactionType } from '../../domain/Transaction';
import type { ICreateTransactionPayload } from '../../domain/Transaction';

interface TransactionPageProps {
  onNavigateToCategories?: () => void;
}

function getLocalDateString(date: Date): string {
  const year = String(date.getFullYear()).padStart(4, '0');
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

export default function TransactionPage({ onNavigateToCategories }: TransactionPageProps) {
  // 1. 同時引入兩個 Store 的狀態與方法
  const { transactions, isLoading: isTxLoading, fetchTransactions, addTransaction, removeTransaction } = useTransactionStore();
  const { accounts, fetchAccounts } = useAccountStore();
  const {
    categories,
    isLoading: isCategoryLoading,
    error: categoryError,
    fetchCategories,
  } = useCategoryStore();

  // 2. 初始化表單狀態 (日期預設為今天)
  const today = getLocalDateString(new Date());
  const [formData, setFormData] = useState<ICreateTransactionPayload>({
    accountId: '',
    amount: 0,
    type: TransactionType.Expense,
    description: '',
    transactionDate: today,
  });

  // 3. 元件載入時，同時拉取交易紀錄與帳戶列表
  useEffect(() => {
    fetchTransactions();
    fetchAccounts();
    void fetchCategories().catch(() => undefined);
  }, [fetchTransactions, fetchAccounts, fetchCategories]);

  // 當帳戶列表載入完成，且表單還沒有選定帳戶時，預設選擇第一個帳戶
  useEffect(() => {
    if (accounts && accounts.length > 0 && !formData.accountId) {
      setFormData(prev => ({ ...prev, accountId: accounts[0].id }));
    }
  }, [accounts, formData.accountId]);

  // 4. 處理表單送出
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.accountId || formData.amount <= 0) {
      alert('請選擇帳戶並輸入大於 0 的金額');
      return;
    }
    if (
      formData.type === TransactionType.Expense &&
      !formData.categoryId
    ) {
      alert('支出交易必須選擇分類');
      return;
    }
    if (formData.type === TransactionType.Transfer) {
      if (accounts.length < 2) {
        alert('轉帳需要至少兩個帳戶，請先新增第二個帳戶。');
        return;
      }
      if (!formData.toAccountId) {
        alert('請選擇轉入帳戶');
        return;
      }
      if (formData.accountId === formData.toAccountId) {
        alert('來源帳戶與轉入帳戶不可相同');
        return;
      }
    }
    
    try {
      const payload = { ...formData };
      if (payload.type !== TransactionType.Transfer) {
        delete payload.toAccountId;
      } else {
        delete payload.categoryId;
      }
      if (!payload.categoryId) {
        delete payload.categoryId;
      }
      await addTransaction(payload);
      // 新增成功後清空表單 (保留日期與帳戶，方便連續記帳)
      setFormData(prev => ({ ...prev, amount: 0, description: '' }));
    } catch (error) {
      alert('新增交易失敗，請檢查網路連線');
    }
  };

  // 輔助方法：將交易類型轉為中文
  const getTypeName = (type: number) => {
    if (type === TransactionType.Expense) return '🔴 支出';
    if (type === TransactionType.Income) return '🟢 收入';
    if (type === TransactionType.Transfer) return '🔵 轉帳';
    return '未知';
  };

  // 輔助方法：根據 ID 找帳戶名稱
  const getAccountName = (id: string) => {
    const account = (accounts || []).find(a => a.id === id);
    return account ? account.name : '未知帳戶';
  };

  return (
    <div style={{ padding: '20px', maxWidth: '600px', margin: '0 auto' }}>
      <h2>📝 收支記帳</h2>

      {/* 記帳表單 */}
      <form onSubmit={handleSubmit} style={{ marginBottom: '20px', padding: '15px', border: '1px solid #ccc', borderRadius: '8px' }}>
        <h3>新增一筆紀錄</h3>
        
        <div style={{ marginBottom: '10px' }}>
          <label>日期：</label>
          <input 
            type="date" 
            value={formData.transactionDate} 
            onChange={(e) => setFormData({ ...formData, transactionDate: e.target.value })} 
            required 
          />
        </div>

        <div style={{ marginBottom: '10px' }}>
          <label>類型：</label>
          <select 
            value={formData.type} 
            onChange={(e) => {
              const type = Number(e.target.value) as typeof TransactionType[keyof typeof TransactionType];
              setFormData((previous) => ({
                ...previous,
                type,
                categoryId: type === TransactionType.Transfer ? undefined : previous.categoryId,
                toAccountId: type === TransactionType.Transfer ? previous.toAccountId : undefined,
              }));
            }}
          >
            <option value={TransactionType.Expense}>支出</option>
            <option value={TransactionType.Income}>收入</option>
            <option value={TransactionType.Transfer}>轉帳</option>
          </select>
        </div>

        <div style={{ marginBottom: '10px' }}>
          <label>{formData.type === TransactionType.Transfer ? '來源帳戶：' : '帳戶：'}</label>
          <select 
            value={formData.accountId} 
            onChange={(e) => setFormData(prev => ({
              ...prev,
              accountId: e.target.value,
              toAccountId: prev.toAccountId === e.target.value ? undefined : prev.toAccountId,
            }))}
            required
          >
            <option value="" disabled>請選擇帳戶</option>
            {(accounts || []).map(acc => (
              <option key={acc.id} value={acc.id}>{acc.name}</option>
            ))}
          </select>
        </div>

        {formData.type === TransactionType.Transfer && (
          <div style={{ marginBottom: '10px' }}>
            <label>轉入帳戶：</label>
            <select
              value={formData.toAccountId ?? ''}
              onChange={(e) => setFormData({ ...formData, toAccountId: e.target.value })}
              required
            >
              <option value="" disabled>請選擇轉入帳戶</option>
              {(accounts || []).map(acc => (
                <option key={acc.id} value={acc.id} disabled={acc.id === formData.accountId}>{acc.name}</option>
              ))}
            </select>
          </div>
        )}

        {formData.type !== TransactionType.Transfer && (
          <div style={{ marginBottom: '10px' }}>
            <label>分類：</label>
            <select
              value={formData.categoryId ?? ''}
              required={formData.type === TransactionType.Expense}
              onChange={(e) => setFormData({
                ...formData,
                categoryId: e.target.value || undefined,
              })}
            >
              {formData.type === TransactionType.Expense ? (
                <option value="" disabled>請選擇分類</option>
              ) : (
                <option value="">未分類（選填）</option>
              )}
              {(categories || []).map((category) => (
                <option key={category.id} value={category.id}>
                  {category.icon} {category.name}
                </option>
              ))}
            </select>
            {isCategoryLoading && <p>載入分類中…</p>}
            {!isCategoryLoading && categories.length === 0 && (
              <p>
                尚未建立分類
                {onNavigateToCategories && (
                  <button type="button" onClick={onNavigateToCategories}>前往分類管理</button>
                )}
              </p>
            )}
            {categoryError && <p role="alert">{categoryError}</p>}
          </div>
        )}

        <div style={{ marginBottom: '10px' }}>
          <label>金額：</label>
          <input 
            type="number" 
            value={formData.amount || ''} 
            onChange={(e) => setFormData({ ...formData, amount: Number(e.target.value) })} 
            required 
            min="1"
          />
        </div>

        <div style={{ marginBottom: '10px' }}>
          <label>備註：</label>
          <input 
            type="text" 
            value={formData.description || ''} 
            onChange={(e) => setFormData({ ...formData, description: e.target.value })} 
            placeholder="例如：午餐、買飲料..."
          />
        </div>

        <button
          type="submit"
          disabled={
            isTxLoading ||
            (accounts || []).length === 0 ||
            (formData.type === TransactionType.Transfer && (accounts || []).length < 2)
          }
        >
          {isTxLoading ? '儲存中...' : '新增紀錄'}
        </button>
        {(!accounts || accounts.length === 0) && (
          <p style={{ color: 'red', fontSize: '14px', marginTop: '5px' }}>請先至「帳戶管理」新增至少一個帳戶才能記帳。</p>
        )}
        {formData.type === TransactionType.Transfer && accounts.length < 2 && (
          <p style={{ color: 'red', fontSize: '14px', marginTop: '5px' }}>轉帳需要至少兩個帳戶，請先新增第二個帳戶。</p>
        )}
      </form>

      {/* 交易紀錄列表 */}
      <h3>近期紀錄</h3>
      {isTxLoading && transactions.length === 0 ? (
        <p>載入中...</p>
      ) : !Array.isArray(transactions) || transactions.length === 0 ? (
        <p>目前還沒有任何紀錄，開始記帳吧！</p>
      ) : (
        <ul style={{ listStyleType: 'none', padding: 0 }}>
          {transactions.map((tx, index) => {
            const amount = tx?.amount ?? 0;
            const txDate = tx?.transactionDate ? new Date(tx.transactionDate).toLocaleDateString() : '未知日期';
            
            return (
              <li key={tx?.id || index} style={{ padding: '10px', borderBottom: '1px solid #eee', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <div>
                  <div style={{ fontSize: '0.85em', color: '#888' }}>{txDate}</div>
                  <strong>{getTypeName(tx?.type)}</strong>
                  <span style={{ margin: '0 10px' }}>{tx?.description || '無備註'}</span>
                  {tx?.type === TransactionType.Transfer ? (
                    <span style={{ fontSize: '0.85em', color: '#666', backgroundColor: '#f0f0f0', padding: '2px 6px', borderRadius: '4px' }}>
                      {getAccountName(tx.accountId)}
                      <br />
                      ↓
                      <br />
                      {tx.toAccountName || getAccountName(tx.toAccountId || '')}
                    </span>
                  ) : (
                    <span style={{ fontSize: '0.85em', color: '#666', backgroundColor: '#f0f0f0', padding: '2px 6px', borderRadius: '4px' }}>
                      {getAccountName(tx?.accountId)}
                    </span>
                  )}
                </div>
                <div style={{ display: 'flex', alignItems: 'center', gap: '15px' }}>
                  <div style={{ color: tx?.type === TransactionType.Income ? 'green' : 'red', fontWeight: 'bold' }}>
                    {tx?.type === TransactionType.Income ? '+' : '-'}$ {amount.toLocaleString()}
                  </div>
                  <button onClick={() => removeTransaction(tx.id)} style={{ color: 'red', cursor: 'pointer', background: 'none', border: 'none' }}>
                    刪除
                  </button>
                </div>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}