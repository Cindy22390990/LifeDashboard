import { create } from 'zustand';
import type { ITransaction, ICreateTransactionPayload } from '../../domain/Transaction';
import { TransactionRepository } from '../../infrastructure/api/TransactionRepository';

// 1. 定義 Transaction Store 的型別介面
interface TransactionState {
  transactions: ITransaction[];
  isLoading: boolean;
  
  // 載入交易紀錄
  fetchTransactions: () => Promise<void>;
  
  // 新增交易紀錄
  addTransaction: (payload: ICreateTransactionPayload) => Promise<void>;
  
  // 刪除交易紀錄
  removeTransaction: (id: string) => Promise<void>;
}

// 2. 實作 Store 邏輯
export const useTransactionStore = create<TransactionState>((set, get) => ({
  transactions: [],
  isLoading: false,

  fetchTransactions: async () => {
    set({ isLoading: true });
    try {
      const data = await TransactionRepository.getAll();
      set({ transactions: data || [], isLoading: false });
    } catch (error) {
      console.error("無法取得交易紀錄列表", error);
      // 發生錯誤時重置為空陣列，避免白畫面
      set({ isLoading: false, transactions: [] }); 
    }
  },

  addTransaction: async (payload) => {
    try {
      await TransactionRepository.create(payload);
      // 新增成功後，直接呼叫 fetchTransactions 重新向後端拉取最新清單
      await get().fetchTransactions();
    } catch (error) {
      console.error("新增交易紀錄失敗", error);
      throw error; // 將錯誤往上拋，讓 UI 層可以 catch 並顯示 alert
    }
  },

  removeTransaction: async (id) => {
    try {
      await TransactionRepository.delete(id);
      // 刪除成功後，從本地 Store 移除該筆紀錄
      set((state) => ({
        transactions: (state.transactions || []).filter(t => t.id !== id)
      }));
    } catch (error) {
      console.error("刪除交易紀錄失敗", error);
      throw error;
    }
  }
}));