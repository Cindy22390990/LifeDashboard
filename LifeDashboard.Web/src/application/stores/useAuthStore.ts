import { create } from 'zustand';

interface AuthState {
  token: string | null;
  userName: string | null;
  isAuthenticated: boolean;
  setSession: (token: string, userName: string) => void;
  logout: () => void;
}

const storedToken = localStorage.getItem('token');
const storedUserName = localStorage.getItem('userName');

export const useAuthStore = create<AuthState>((set) => ({
  token: storedToken,
  userName: storedUserName,
  isAuthenticated: Boolean(storedToken),

  setSession: (token, userName) => {
    localStorage.setItem('token', token);
    localStorage.setItem('userName', userName);
    set({
      token,
      userName,
      isAuthenticated: true,
    });
  },

  logout: () => {
    localStorage.removeItem('token');
    localStorage.removeItem('userName');
    set({
      token: null,
      userName: null,
      isAuthenticated: false,
    });
  },
}));
