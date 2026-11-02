import {
  BrowserRouter,
  Navigate,
  Route,
  Routes,
  useLocation,
} from 'react-router-dom';
import App from './App';
import { useAuthStore } from './application/stores/authStore';
import LoginPage from './presentation/pages/LoginPage';
import RegisterPage from './presentation/pages/RegisterPage';
import RequireAuth from './presentation/RequireAuth';

function Dashboard() {
  const userName = useAuthStore((state) => state.userName);
  const logout = useAuthStore((state) => state.logout);

  return (
    <>
      <header style={{ display: 'flex', justifyContent: 'flex-end', gap: 12, padding: 12 }}>
        <span>{userName}</span>
        <button onClick={logout}>登出</button>
      </header>
      <App />
    </>
  );
}

function LoginRoute() {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const location = useLocation();
  const registrationSuccess = location.state?.registrationSuccess === true;

  return isAuthenticated && !registrationSuccess
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
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route path="/dashboard" element={<Dashboard />} />
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
