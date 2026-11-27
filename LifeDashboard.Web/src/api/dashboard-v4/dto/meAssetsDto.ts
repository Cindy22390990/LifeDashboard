export interface MeAccountItemDto {
  id: string;
  name: string;
  accountType: 'Cashflow' | 'Saving' | 'Invest';
  balance: number;
}

export interface MeAssetsDto {
  totalAssets: number;
  accounts: MeAccountItemDto[];
}
