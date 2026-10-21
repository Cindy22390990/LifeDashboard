import http from './http';
import type { ITransaction, ICreateTransactionPayload } from '../../domain/Transaction';

export const TransactionRepository = {
  // 取得所有交易紀錄
  getAll: async (): Promise<ITransaction[]> => {
    return await http.get<ITransaction[]>('/Transaction');
  },


  // 新增一筆交易 (支出/收入)
   create: async (
    data: ICreateTransactionPayload
  ): Promise<ITransaction> => {

    return await http.post<ITransaction>(
      '/Transaction',
      data
    );
  },

  // 刪除交易
  delete: async (id: string): Promise<void> => {
    await http.delete(`/Transaction/${id}`);
  }

};