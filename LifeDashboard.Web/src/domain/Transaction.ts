
// 定義交易類型 (對應後端的 Enum)
export const TransactionType = {
  Expense: 1,  // 支出
  Income: 2,   // 收入
  Transfer: 3  // 轉帳
}as const;

// 讓 TransactionType 也可以當作型別來使用 (值只能是 1, 2 或 3)
export type TransactionType = typeof TransactionType[keyof typeof TransactionType];
// 從後端接收到的完整交易資料結構
export interface ITransaction {
  id: string;
  accountId: string;       // 扣款/入帳的帳戶 ID
  amount: number;          // 金額
  type: TransactionType;   // 收支類型
  categoryId?: string;     // 分類 ID (選填，之後可擴充分類系統)
  description: string;     // 備註說明
  transactionDate: string; // 交易日期
}

// 傳送給後端的新增交易資料結構
export interface ICreateTransactionPayload {
  accountId: string;       // 扣款/入帳的帳戶 ID
  amount: number;          // 金額
  type: TransactionType;   // 收支類型
  categoryId?: string;     // 分類 ID (選填，之後可擴充分類系統)
  description: string;     // 備註說明
  transactionDate: string; // 交易日期
}