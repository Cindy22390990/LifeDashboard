import { useEffect, useState } from 'react';
import { useAccountStore } from '../../application/stores/accountStore';
import { AccountType } from '../../domain/Account';
import type { ICreateAccountPayload } from '../../domain/Account';

export default function AccountPage() {
  const { accounts, isLoading, fetchAccounts, addAccount, removeAccount } = useAccountStore();
  const [formData, setFormData] = useState<ICreateAccountPayload>({
    name: '',
    type: AccountType.Cashflow,
    initialBalance: 0,
  });

  // 元件載入時，自動抓取帳戶列表
  useEffect(() => {
    fetchAccounts();
  }, [fetchAccounts]);

  // 處理表單送出
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.name) return;
    
    try {
      await addAccount(formData);
      // 新增成功後清空表單
      setFormData({ name: '', type: AccountType.Cashflow, initialBalance: 0 });
    } catch (error) {
      alert('新增失敗，請檢查網路連線');
    }
  };

  // 將 Enum 轉換為可讀的中文標籤
  const getAccountTypeName = (type: number) => {
    switch (type) {
      case AccountType.Cashflow: return '日常現金流';
      case AccountType.Saving: return '儲備/預備金';
      case AccountType.Invest: return '長期投資';
      default: return '未知';
    }
  };

  return (
    <div style={{ padding: '20px', maxWidth: '600px', margin: '0 auto' }}>
      <h2>💰 帳戶管理</h2>

      {/* 新增帳戶表單 */}
      <form onSubmit={handleSubmit} style={{ marginBottom: '20px', padding: '15px', border: '1px solid #ccc', borderRadius: '8px' }}>
        <h3>新增帳戶</h3>
        <div style={{ marginBottom: '10px' }}>
          <label>帳戶名稱：</label>
          <input 
            type="text" 
            value={formData.name} 
            onChange={(e) => setFormData({ ...formData, name: e.target.value })} 
            required 
          />
        </div>
        <div style={{ marginBottom: '10px' }}>
          <label>帳戶類型：</label>
          <select 
            value={formData.type} 
            onChange={(e) => setFormData({ ...formData, type: Number(e.target.value) as AccountType })}
          >
            <option value={AccountType.Cashflow}>日常現金流</option>
            <option value={AccountType.Saving}>儲備/預備金</option>
            <option value={AccountType.Invest}>長期投資</option>
          </select>
        </div>
        <div style={{ marginBottom: '10px' }}>
          <label>初始餘額：</label>
          <input 
            type="number" 
            value={formData.initialBalance} 
            onChange={(e) => setFormData({ ...formData, initialBalance: Number(e.target.value) })} 
            required 
          />
        </div>
        <button type="submit">新增帳戶</button>
      </form>

      {/* 帳戶列表展示 */}
      <h3>我的帳戶列表</h3>
      {isLoading ? (
        <p>載入中...</p>
      ) : !Array.isArray(accounts) || accounts.length === 0 ? (
        <p>目前還沒有任何帳戶，請先新增一個！</p>
      ) : (
        <ul style={{ listStyleType: 'none', padding: 0 }}>
          {accounts.map((acc, index) => {
            // 防呆機制：如果後端沒有傳回 currentBalance，預設給 0，避免 .toLocaleString() 崩潰
            const balance = acc?.currentBalance ??  0;
            
            return (
           <li key={acc?.id || index} style={{ padding: '10px', borderBottom: '1px solid #eee', display: 'flex', justifyContent: 'space-between' }}>
                <div>
                  <strong>{acc?.name || '未命名'}</strong> 
                  <span style={{ marginLeft: '10px', fontSize: '0.85em', color: '#666' }}>
                    ({getAccountTypeName(acc?.type)})
                  </span>
                  <div style={{ color: 'green' }}>
                    $ {balance.toLocaleString()}
                  </div>
                </div>
                {!acc?.isSystemAccount && acc?.id && (
                  <button onClick={() => removeAccount(acc.id)} style={{ color: 'red', cursor: 'pointer' }}>
                    刪除
                  </button>
                )}
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}