import http from '../../infrastructure/api/http';
import { toApiError } from './apiError';
import type { DashboardV4Client } from '../dashboard-v4/clients/dashboardV4Client';
import type { DashboardV4HomeDto } from '../dashboard-v4/dto/homeDto';
import type { MeAssetsDto } from '../dashboard-v4/dto/meAssetsDto';
import type { AccountsReadDto } from '../dashboard-v4/dto/accountsReadDto';
import type { CategoryReadItemDto } from '../dashboard-v4/dto/categoriesReadDto';
import type {
  QuickCreateTransactionRequest,
  TransactionListFilter,
  TransactionListResponseDto,
} from '../dashboard-v4/dto/transactionDto';

async function request<T>(operation: () => Promise<T>): Promise<T> {
  try {
    return await operation();
  } catch (error) {
    throw toApiError(error);
  }
}

export const dashboardV4Client: DashboardV4Client = {
  getHome: () =>
    request(() => http.get<DashboardV4HomeDto>('/dashboard/v4/home')),

  getTransactions: (filter: TransactionListFilter) =>
    request(() =>
      http.get<TransactionListResponseDto>('/transactions', {
        params: filter,
      }),
    ),

  getMeAssets: () =>
    request(() => http.get<MeAssetsDto>('/me/assets')),

  getAccounts: () =>
    request(() => http.get<AccountsReadDto>('/accounts')),

  getCategories: () =>
    request(() => http.get<CategoryReadItemDto[]>('/categories')),

  quickCreate: (body: QuickCreateTransactionRequest) =>
    request(() =>
      http.post<TransactionListResponseDto['items'][number]>(
        '/transactions/quick',
        body,
      ),
    ),
};
