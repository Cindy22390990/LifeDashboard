import { create } from 'zustand';

interface AuthState {
  token: string | null;
  username: string | null;
  isAuthenticated: boolean;
  login: (token: string, username: string) => void;
  logout: () => void;
}

const tokenStorageKey = 'lifedashboard_token';
const usernameStorageKey = 'lifedashboard_username';
const storedToken = localStorage.getItem(tokenStorageKey) ?? localStorage.getItem('token');
const storedUsername = localStorage.getItem(usernameStorageKey) ?? localStorage.getItem('userName');

if (storedToken) localStorage.setItem(tokenStorageKey, storedToken);
if (storedUsername) localStorage.setItem(usernameStorageKey, storedUsername);
localStorage.removeItem('token');
localStorage.removeItem('userName');

export const useAuthStore = create<AuthState>((set) => ({
  token: storedToken,
  username: storedUsername,
  isAuthenticated: Boolean(storedToken),
  login: (token, username) => {
    localStorage.setItem(tokenStorageKey, token);
    localStorage.setItem(usernameStorageKey, username);
    set({ token, username, isAuthenticated: true });
  },
  logout: () => {
    localStorage.removeItem(tokenStorageKey);
    localStorage.removeItem(usernameStorageKey);
    localStorage.removeItem('token');
    localStorage.removeItem('userName');
    set({ token: null, username: null, isAuthenticated: false });
  },
}));
