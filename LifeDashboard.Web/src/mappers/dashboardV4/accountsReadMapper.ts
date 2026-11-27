import type { AccountsReadDto } from '../../api/dashboard-v4/dto/accountsReadDto';
import type {
  V4AccountsReadViewModel,
  V4AccountType,
} from './viewModels';

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

export function mapV4AccountsRead(
  dto: AccountsReadDto,
): V4AccountsReadViewModel {
  return {
    totalAssets: dto.totalAssets,
    items: (dto.items ?? []).map((account) => ({
      id: account.id,
      name: account.name?.trim() || 'Unnamed Account',
      accountType: mapAccountType(account.accountType),
      balance: account.balance,
    })),
  };
}
