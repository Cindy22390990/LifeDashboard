export interface AccountReadItemDto {
  id: string;
  name: string;
  accountType: 'Cashflow' | 'Saving' | 'Invest' | 'Unknown';
  balance: number;
}

export interface AccountsReadDto {
  totalAssets: number;
  items: AccountReadItemDto[];
}
