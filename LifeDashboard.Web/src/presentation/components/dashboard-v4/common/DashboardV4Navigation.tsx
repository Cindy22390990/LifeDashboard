import { NavLink } from 'react-router-dom';
import './FeaturePages.css';

const navigationItems = [
  { label: '首頁', to: '/dashboard-v4/home' },
  { label: '交易明細', to: '/dashboard-v4/transactions' },
  { label: '帳戶', to: '/dashboard-v4/accounts' },
  { label: '目標', to: '/dashboard-v4/goals' },
  { label: '報表', to: '/dashboard-v4/reports' },
  { label: '預算', to: '/dashboard-v4/budget' },
  { label: '固定支出', to: '/dashboard-v4/recurring' },
  { label: '分類', to: '/dashboard-v4/categories' },
  { label: '設定', to: '/dashboard-v4/settings' },
] as const;

export function DashboardV4Navigation() {
  return (
    <nav
      aria-label="Dashboard V4 功能導覽"
      className="dashboard-v4-navigation"
    >
      {navigationItems.map((item) => (
        <NavLink
          className={({ isActive }) =>
            `dashboard-v4-navigation__link${isActive ? ' is-active' : ''}`
          }
          key={item.to}
          to={item.to}
        >
          {item.label}
        </NavLink>
      ))}
    </nav>
  );
}
