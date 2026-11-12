import { useState } from 'react';
import { Link, NavLink, Outlet, useLocation } from 'react-router-dom';
import { SystemSettingPage } from './presentation/pages/SystemSettingPage';
import AccountPage from './presentation/pages/AccountPage';
import './AppShell.css';

type SettingsModal = 'accounts' | 'system' | null;

const primaryTabs = [
  { to: '/dashboard', label: 'Dashboard', icon: '⌂' },
  { to: '/responsibilities', label: 'Responsibilities', icon: '▣' },
  { to: '/goals', label: 'Goal', icon: '◎' },
  { to: '/stats', label: 'Stats', icon: '▤' },
];

export default function App() {
  const [settingsModal, setSettingsModal] = useState<SettingsModal>(null);
  const location = useLocation();
  const isResponsibilitiesPage = location.pathname === '/responsibilities';

  const closeModal = () => setSettingsModal(null);

  return (
    <div className="app-shell">
      <div className="app-utility-nav">
        <Link to="/transactions">交易記錄／新增交易</Link>
        {!isResponsibilitiesPage && (
          <button type="button" onClick={() => setSettingsModal('accounts')}>
            帳戶管理
          </button>
        )}
        <button type="button" onClick={() => setSettingsModal('system')}>
          系統設定
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
          <NavLink
            key={tab.to}
            to={tab.to}
            end
            className={({ isActive }) =>
              `bottom-tab${isActive ? ' bottom-tab-active' : ''}`
            }
          >
            <span className="bottom-tab-icon" aria-hidden="true">{tab.icon}</span>
            <span>{tab.label}</span>
          </NavLink>
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
