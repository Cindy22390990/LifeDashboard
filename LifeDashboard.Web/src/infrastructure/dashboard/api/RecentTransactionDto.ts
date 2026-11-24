export interface RecentTransactionDto {
  id: string;
  note: string | null;
  categoryName: string;
  accountName: string;
  amount: number;
  type: number;
  transactionType: string;
  transactionDate: string;
}
