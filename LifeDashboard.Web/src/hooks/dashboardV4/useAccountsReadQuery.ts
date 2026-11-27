import { useQuery } from '@tanstack/react-query';
import { dashboardV4Client } from '../../api/dashboardV4/dashboardV4Client';
import type { AccountsReadDto } from '../../api/dashboard-v4/dto/accountsReadDto';
import type { ApiError } from '../../api/dashboardV4/apiError';
import { dashboardV4Keys } from '../../api/queryKeys';
import {
  mapV4AccountsRead,
  type V4AccountsReadViewModel,
} from '../../mappers/dashboardV4';

export function useAccountsReadQuery() {
  return useQuery<AccountsReadDto, ApiError, V4AccountsReadViewModel>({
    queryKey: dashboardV4Keys.accounts.all(),
    queryFn: () => dashboardV4Client.getAccounts(),
    select: mapV4AccountsRead,
  });
}
