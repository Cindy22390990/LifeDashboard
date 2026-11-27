import type { MeAssetsDto } from '../../api/dashboard-v4/dto/meAssetsDto';
import type { V4AccountType, V4MeAssetsViewModel } from './viewModels';

function mapAccountType(value: string | null | undefined): V4AccountType {
  switch (value?.trim().toLowerCase()) {
    case 'cashflow':
      return 'cashflow';
    case 'saving':
      return 'saving';
    case 'invest':
      return 'invest';
    default:
      return 'unknown';
  }
}

export function mapV4MeAssets(dto: MeAssetsDto): V4MeAssetsViewModel {
  return {
    totalAssets: dto.totalAssets,
    accounts: (dto.accounts ?? []).map((account) => ({
      id: account.id,
      name: account.name,
      accountType: mapAccountType(account.accountType),
      balance: account.balance,
    })),
  };
}
