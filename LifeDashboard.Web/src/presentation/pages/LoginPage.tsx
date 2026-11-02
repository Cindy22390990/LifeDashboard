import { useState } from 'react';
import type { FormEvent } from 'react';
import axios from 'axios';
import { Navigate, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../../application/stores/useAuthStore';
import { AuthRepository } from '../../infrastructure/api/AuthRepository';

function getLoginErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as { message?: string } | undefined;
    return data?.message ?? '登入失敗';
  }

  return '登入失敗';
}

export function LoginPage() {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const setSession = useAuthStore((state) => state.setSession);
  const navigate = useNavigate();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  if (isAuthenticated) {
    return <Navigate to="/dashboard" replace />;
  }

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);

    try {
      const result = await AuthRepository.login(username, password);
      setSession(result.token, result.userName);
      navigate('/dashboard', { replace: true });
    } catch (loginError) {
      setError(getLoginErrorMessage(loginError));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div style={{ maxWidth: '420px', margin: '80px auto', fontFamily: 'sans-serif' }}>
      <h2>登入</h2>
      <form
        onSubmit={handleSubmit}
        style={{ display: 'flex', flexDirection: 'column', gap: '12px', border: '1px solid #ccc', padding: '20px', borderRadius: '8px' }}
      >
        <label>
          帳號
          <input
            type="text"
            value={username}
            onChange={(event) => setUsername(event.target.value)}
            required
            autoComplete="username"
            style={{ display: 'block', width: '100%', marginTop: '4px' }}
          />
        </label>
        <label>
          密碼
          <input
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            required
            autoComplete="current-password"
            style={{ display: 'block', width: '100%', marginTop: '4px' }}
          />
        </label>
        {error && <div style={{ color: 'red' }}>{error}</div>}
        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? '登入中...' : '登入'}
        </button>
      </form>
    </div>
  );
}
