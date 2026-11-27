import type { DashboardV4HomeDto } from '../dto/homeDto';
import type {
  QuickCreateTransactionRequest,
  TransactionListFilter,
  TransactionListResponseDto,
} from '../dto/transactionDto';
import type { MeAssetsDto } from '../dto/meAssetsDto';
import type { AccountsReadDto } from '../dto/accountsReadDto';
import type { CategoryReadItemDto } from '../dto/categoriesReadDto';

export interface DashboardV4Client {
  getHome: () => Promise<DashboardV4HomeDto>;
  getTransactions: (
    filter: TransactionListFilter,
  ) => Promise<TransactionListResponseDto>;
  getMeAssets: () => Promise<MeAssetsDto>;
  getAccounts: () => Promise<AccountsReadDto>;
  getCategories: () => Promise<CategoryReadItemDto[]>;
  quickCreate: (
    request: QuickCreateTransactionRequest,
  ) => Promise<TransactionListResponseDto['items'][number]>;
}
