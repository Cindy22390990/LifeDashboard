import {
  BrowserRouter,
  Navigate,
  Route,
  Routes,
} from 'react-router-dom';
import App from './App';
import { useAuthStore } from './application/stores/authStore';
import { apiDashboardDataSource } from './infrastructure/dashboard/api/ApiDashboardDataSource';
import AccountPage from './presentation/pages/AccountPage';
import DashboardV3Page from './presentation/pages/DashboardV3Page';
import HomePage from './presentation/pages/HomePage';
import PlanPage from './presentation/pages/PlanPage';
import MePage from './presentation/pages/MePage';
import { TransactionsPage as DashboardV4TransactionsPage } from './presentation/pages/dashboard-v4/transactions/TransactionsPage';
import { AccountsPage } from './presentation/pages/dashboard-v4/accounts/AccountsPage';
import { GoalsPage } from './presentation/pages/dashboard-v4/goals/GoalsPage';
import { ReportsPage } from './presentation/pages/dashboard-v4/reports/ReportsPage';
import { BudgetPage } from './presentation/pages/dashboard-v4/budget/BudgetPage';
import { RecurringExpensesPage } from './presentation/pages/dashboard-v4/recurring/RecurringExpensesPage';
import { CategoriesPage } from './presentation/pages/dashboard-v4/categories/CategoriesPage';
import { SettingsPage } from './presentation/pages/dashboard-v4/settings/SettingsPage';
import FixedCommitmentPage from './presentation/pages/FixedCommitmentPage';
import GoalPage from './presentation/pages/GoalPage';
import LoginPage from './presentation/pages/LoginPage';
import MonthlyReportPage from './presentation/pages/MonthlyReportPage';
import RegisterPage from './presentation/pages/RegisterPage';
import TransactionPage from './presentation/pages/TransactionPage';
import RequireAuth from './presentation/RequireAuth';

function Dashboard() {
  const username = useAuthStore((state) => state.username);
  const logout = useAuthStore((state) => state.logout);

  return (
    <>
      <header className="app-user-nav">
        <span>{username}</span>
        <button onClick={logout}>Logout</button>
      </header>
      <App />
    </>
  );
}

function LoginRoute() {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);

  return isAuthenticated
    ? <Navigate to="/dashboard" replace />
    : <LoginPage />;
}

export default function AuthApp() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginRoute />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route element={<RequireAuth />}>
          <Route
            path="/dashboard"
            element={<DashboardV3Page dataSource={apiDashboardDataSource} />}
          />
          <Route
            path="/dashboard-v3"
            element={<Navigate to="/dashboard" replace />}
          />
          <Route path="/dashboard-v4/home" element={<HomePage />} />
          <Route
            path="/dashboard-v4/transactions"
            element={<DashboardV4TransactionsPage />}
          />
          <Route path="/dashboard-v4/accounts" element={<AccountsPage />} />
          <Route path="/dashboard-v4/goals" element={<GoalsPage />} />
          <Route path="/dashboard-v4/reports" element={<ReportsPage />} />
          <Route path="/dashboard-v4/budget" element={<BudgetPage />} />
          <Route path="/dashboard-v4/recurring" element={<RecurringExpensesPage />} />
          <Route path="/dashboard-v4/categories" element={<CategoriesPage />} />
          <Route path="/dashboard-v4/settings" element={<SettingsPage />} />
          <Route path="/dashboard-v4/plan" element={<PlanPage />} />
          <Route path="/dashboard-v4/me" element={<MePage />} />
          <Route element={<Dashboard />}>
            <Route index element={<Navigate to="/dashboard" replace />} />
            <Route path="/responsibilities" element={<FixedCommitmentPage />} />
            <Route path="/goals" element={<GoalPage />} />
            <Route path="/stats" element={<MonthlyReportPage />} />
            <Route path="/transactions" element={<TransactionPage />} />
            <Route path="/accounts" element={<AccountPage />} />
          </Route>
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
