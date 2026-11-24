export interface RecentTransactionDto {
  id: string;
  note?: string | null;
  categoryName: string;
  accountName: string;
  amount: number;
  type: string | number;
  transactionDate: string;
}
