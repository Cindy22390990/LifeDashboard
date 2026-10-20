// 定義帳戶的屬性列舉 (對應後端的 AccountType)
export const AccountType = {
  Cashflow: 1,  // 日常現金流
  Saving: 2,    // 儲備/預備金
  Invest: 3     // 長期投資
} as const;

export type AccountType = typeof AccountType[keyof typeof AccountType];
// 定義帳戶資料結構 (對應後端的 AccountDto)
export interface IAccount {
  id: string;
  name: string;
  type: AccountType;
  currentBalance: number;
  isSystemAccount: boolean;
}

// 建立帳戶時的 Payload
export interface ICreateAccountPayload {
  name: string;
  type: AccountType;
  initialBalance: number;
}