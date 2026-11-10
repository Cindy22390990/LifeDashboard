import { useState, type FormEvent } from 'react';
import axios from 'axios';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../../application/stores/authStore';
import { AuthRepository } from '../../infrastructure/api/AuthRepository';

export default function LoginPage() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [errorMessage, setErrorMessage] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const login = useAuthStore((state) => state.login);
  const navigate = useNavigate();
  const location = useLocation();
  const registrationSuccess = location.state?.registrationSuccess === true;

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setErrorMessage('');
    setIsSubmitting(true);

    try {
      const result = await AuthRepository.login(username, password);
      login(result.token, result.userName);
      navigate('/dashboard', { replace: true });
    } catch (error: unknown) {
      if (axios.isAxiosError<{ message?: string }>(error)) {
        setErrorMessage(error.response?.data?.message ?? '登入失敗，請稍後再試。');
      } else {
        setErrorMessage('登入時發生非預期錯誤。');
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <main style={{ maxWidth: 360, margin: '10vh auto', padding: 24 }}>
      <h1>登入 LifeDashboard</h1>
      {registrationSuccess && <p role="status">註冊成功，請登入</p>}
      <form onSubmit={handleSubmit}>
        <label htmlFor="username">使用者名稱</label>
        <input
          id="username"
          name="username"
          autoComplete="username"
          required
          value={username}
          onChange={(event) => setUsername(event.target.value)}
          style={{ display: 'block', width: '100%', margin: '8px 0 16px' }}
        />
        <label htmlFor="password">密碼</label>
        <input
          id="password"
          name="password"
          type="password"
          autoComplete="current-password"
          required
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          style={{ display: 'block', width: '100%', margin: '8px 0 16px' }}
        />
        {errorMessage && <p role="alert">{errorMessage}</p>}
        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? '登入中…' : '登入'}
        </button>
      </form>
      <p>
        尚未有帳號？ <Link to="/register">註冊</Link>
      </p>
    </main>
  );
}
