import { create } from 'zustand';
import type { IAccount, ICreateAccountPayload } from '../../domain/Account';
import { AccountRepository } from '../../infrastructure/api/AccountRepository';

interface AccountState {
  accounts: IAccount[];
  isLoading: boolean;
  
  // 載入帳戶列表
  fetchAccounts: () => Promise<void>;
  
  // 新增帳戶
  addAccount: (payload: ICreateAccountPayload) => Promise<void>;
  
  // 刪除帳戶
  removeAccount: (id: string) => Promise<void>;
}

export const useAccountStore = create<AccountState>((set, get) => ({
  accounts: [],
  isLoading: false,

  fetchAccounts: async () => {
    set({ isLoading: true });
    try {
      const data = await AccountRepository.getAll();
      set({ accounts: data || [], isLoading: false });
    } catch (error) {
      console.error("無法取得帳戶列表", error);
      set({ isLoading: false, accounts: [] }); // 發生錯誤時也重置為空陣列
    }
  },

  addAccount: async (payload) => {
    try {
      await AccountRepository.create(payload);
      // 新增成功後，直接重新向後端拉取最完整、最新的清單
      await get().fetchAccounts();
    } catch (error) {
      console.error("新增帳戶失敗", error);
      throw error;
    }
  },

  removeAccount: async (id) => {
    try {
      await AccountRepository.delete(id);
      // 刪除成功後，從本地 Store 移除該帳戶
      set((state) => ({
        accounts: (state.accounts || []).filter(a => a.id !== id)
      }));
    } catch (error) {
      console.error("刪除帳戶失敗", error);
      throw error;
    }
  }
}));