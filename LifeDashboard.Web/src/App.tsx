import { useState } from 'react';
import { SystemSettingPage } from './presentation/pages/SystemSettingPage';
import AccountPage from './presentation/pages/AccountPage';
import TransactionPage from './presentation/pages/TransactionPage';

export default function App() {
  // 使用狀態來控制目前顯示哪個頁面，預設先顯示「帳戶管理」
  const [currentTab, setCurrentTab] = useState<'accounts' | 'settings' | 'transactions'>('transactions');

  // 輔助函式：用來統一按鈕的樣式，讓程式碼更簡潔
  const getTabStyle = (tabName: string) => ({
    padding: '8px 16px', 
    cursor: 'pointer',
    backgroundColor: currentTab === tabName ? '#007BFF' : '#ddd',
    color: currentTab === tabName ? 'white' : 'black',
    border: 'none',
    borderRadius: '4px'
  });
  return (
    <div style={{ fontFamily: 'sans-serif', color: '#333' }}>
      {/* 頂部導覽列 */}
      <nav style={{ padding: '15px 20px', background: '#f5f5f5', marginBottom: '20px', display: 'flex', gap: '15px' }}>
        {/* 3. 加入「收支記帳」按鈕 */}
        <button 
          onClick={() => setCurrentTab('transactions')}
          style={getTabStyle('transactions')}
        >
          📝 收支記帳
        </button>
        <button 
          onClick={() => setCurrentTab('accounts')}
          style={getTabStyle('accounts')}
        >
          💰 帳戶管理
        </button>
        <button 
          onClick={() => setCurrentTab('settings')}
          style={getTabStyle('settings')}
        >
          ⚙️ 系統設定
        </button>
      </nav>

      {/* 主要內容區塊 */}
      <main>
        {/* 4. 根據狀態渲染對應的頁面 */}
        {currentTab === 'transactions' && <TransactionPage />}
        {currentTab === 'accounts' && <AccountPage />}
        {currentTab === 'settings' && <SystemSettingPage />}
      </main>
    </div>
  );
}