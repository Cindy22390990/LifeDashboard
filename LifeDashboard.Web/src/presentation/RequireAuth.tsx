import { Navigate, Outlet } from 'react-router-dom';
import { useAuthStore } from '../application/stores/authStore';

export default function RequireAuth() {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);

  return isAuthenticated
    ? <Outlet />
    : <Navigate to="/login" replace />;
}
