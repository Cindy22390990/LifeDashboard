import http from './http'; // 引入我們之前設定好代理 /api 的 axios 實體
import type { IAccount, ICreateAccountPayload } from '../../domain/Account';

export const AccountRepository = {
  // 取得所有帳戶
  getAll: async (): Promise<IAccount[]> => {
    const response = await http.get<IAccount[]>('/Account');
    return response.data;
  },

  // 新增帳戶
  create: async (data: ICreateAccountPayload): Promise<IAccount> => {
    const response = await http.post<IAccount>('/Account', data);
    return response.data;
  },

  // 軟刪除帳戶
  delete: async (id: string): Promise<void> => {
    await http.delete(`/Account/${id}`);
  }
};