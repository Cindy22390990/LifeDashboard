import http from './http';

export interface LoginResult {
  token: string;
  userName: string;
}

export const AuthRepository = {
  login: async (
    username: string,
    password: string,
  ): Promise<LoginResult> => {
    return await http.post<LoginResult>('/auth/login', {
      username,
      password,
    });
  },
};
