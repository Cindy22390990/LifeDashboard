import { useState } from 'react';
import { Link, Outlet, useLocation } from 'react-router-dom';
import { SystemSettingPage } from './presentation/pages/SystemSettingPage';
import AccountPage from './presentation/pages/AccountPage';
import './AppShell.css';

type SettingsModal = 'accounts' | 'system' | null;

const primaryTabs = [
  { to: '/dashboard', label: 'Dashboard', icon: '⌂' },
  { to: '/transactions', label: 'Transactions', icon: '↕' },
  { to: '/goals', label: 'Goals', icon: '◎' },
  { to: '/accounts', label: 'Accounts', icon: '▣' },
];

export default function App() {
  const [settingsModal, setSettingsModal] = useState<SettingsModal>(null);
  const location = useLocation();

  const closeModal = () => setSettingsModal(null);

  return (
    <div className="app-shell">
      <div className="app-utility-nav">
        <button type="button" onClick={() => setSettingsModal('system')}>
          Settings
        </button>
      </div>

      <main className="app-main">
        <Outlet context={{
          openAccountManagement: () => setSettingsModal('accounts'),
          openSystemSettings: () => setSettingsModal('system'),
        }} />
      </main>

      <nav className="bottom-tab-nav" aria-label="主要導覽">
        {primaryTabs.map((tab) => (
          <Link
            key={tab.to}
            to={tab.to}
            aria-current={location.pathname === tab.to ? 'page' : undefined}
            className={`bottom-tab${location.pathname === tab.to ? ' bottom-tab-active' : ''}`}
          >
            <span className="bottom-tab-icon" aria-hidden="true">{tab.icon}</span>
            <span className="bottom-tab-label">{tab.label}</span>
          </Link>
        ))}
      </nav>

      {settingsModal && (
        <div
          className="app-modal-backdrop"
          onMouseDown={(event) => {
            if (event.target === event.currentTarget) {
              closeModal();
            }
          }}
        >
          <section
            aria-labelledby="app-modal-title"
            aria-modal="true"
            className="app-modal"
            role="dialog"
          >
            <header className="app-modal-header">
              <h2 id="app-modal-title">
                {settingsModal === 'accounts' ? '帳戶管理' : '系統設定'}
              </h2>
              <button type="button" onClick={closeModal} aria-label="關閉">
                關閉
              </button>
            </header>
            {settingsModal === 'accounts'
              ? <AccountPage />
              : <SystemSettingPage />}
          </section>
        </div>
      )}
    </div>
  );
}
