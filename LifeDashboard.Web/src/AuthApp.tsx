import {
  BrowserRouter,
  Navigate,
  Route,
  Routes,
  useNavigate,
} from 'react-router-dom';
import App from './App';
import { useAuthStore } from './application/stores/authStore';
import AccountPage from './presentation/pages/AccountPage';
import DashboardPage from './presentation/pages/DashboardPage';
import DashboardV3Page from './presentation/pages/DashboardV3Page';
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

function DashboardRoute() {
  const navigate = useNavigate();

  return (
    <DashboardPage
      onNavigateToTransactions={() => navigate('/transactions')}
    />
  );
}

export default function AuthApp() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginRoute />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route element={<RequireAuth />}>
          <Route element={<Dashboard />}>
            <Route index element={<Navigate to="/dashboard" replace />} />
            <Route path="/dashboard" element={<DashboardRoute />} />
            <Route path="/responsibilities" element={<FixedCommitmentPage />} />
            <Route path="/goals" element={<GoalPage />} />
            <Route path="/stats" element={<MonthlyReportPage />} />
            <Route path="/transactions" element={<TransactionPage />} />
            <Route path="/accounts" element={<AccountPage />} />
          </Route>
          <Route path="/dashboard-v3" element={<DashboardV3Page />} />
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
