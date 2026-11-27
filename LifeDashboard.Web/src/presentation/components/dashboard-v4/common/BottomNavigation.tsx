import { NavLink } from 'react-router-dom';

const navigationItems = [
  { label: 'Home', to: '/dashboard-v4/home' },
  { label: 'Transactions', to: '/dashboard-v4/transactions' },
  { label: 'Plan', to: '/dashboard-v4/plan' },
  { label: 'Me', to: '/dashboard-v4/me' },
] as const;

export function BottomNavigation() {
  return (
    <nav aria-label="Dashboard V4 navigation">
      {navigationItems.map((item) => (
        <NavLink
          key={item.to}
          to={item.to}
          className={({ isActive }) => isActive ? 'is-active' : undefined}
        >
          {item.label}
        </NavLink>
      ))}
    </nav>
  );
}
