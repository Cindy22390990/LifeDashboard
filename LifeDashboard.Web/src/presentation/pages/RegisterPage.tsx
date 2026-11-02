import { useState, type FormEvent } from 'react';
import axios from 'axios';
import { Link, useNavigate } from 'react-router-dom';
import http from '../../infrastructure/api/http';

export default function RegisterPage() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [errorMessage, setErrorMessage] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const navigate = useNavigate();

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setErrorMessage('');

    const normalizedUsername = username.trim();
    if (!normalizedUsername || !password) {
      setErrorMessage('請輸入使用者名稱與密碼。');
      return;
    }

    setIsSubmitting(true);
    try {
      await http.post('/auth/register', {
        username: normalizedUsername,
        password,
      });
      navigate('/login', {
        replace: true,
        state: { registrationSuccess: true },
      });
    } catch (error: unknown) {
      if (axios.isAxiosError<{ message?: string }>(error)) {
        setErrorMessage(error.response?.data?.message ?? '註冊失敗，請稍後再試。');
      } else {
        setErrorMessage('註冊時發生非預期錯誤。');
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <main style={{ maxWidth: 360, margin: '10vh auto', padding: 24 }}>
      <h1>註冊 LifeDashboard</h1>
      <form onSubmit={handleSubmit}>
        <label htmlFor="register-username">使用者名稱</label>
        <input
          id="register-username"
          name="username"
          autoComplete="username"
          required
          value={username}
          onChange={(event) => setUsername(event.target.value)}
          style={{ display: 'block', width: '100%', margin: '8px 0 16px' }}
        />
        <label htmlFor="register-password">密碼</label>
        <input
          id="register-password"
          name="password"
          type="password"
          autoComplete="new-password"
          required
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          style={{ display: 'block', width: '100%', margin: '8px 0 16px' }}
        />
        {errorMessage && <p role="alert">{errorMessage}</p>}
        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? '註冊中…' : '註冊'}
        </button>
      </form>
      <p>
        已有帳號？ <Link to="/login">登入</Link>
      </p>
    </main>
  );
}
