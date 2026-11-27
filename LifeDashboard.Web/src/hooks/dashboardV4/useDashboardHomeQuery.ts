import { useQuery } from '@tanstack/react-query';
import type { DashboardV4HomeDto } from '../../api/dashboard-v4/dto/homeDto';
import { dashboardV4Client } from '../../api/dashboardV4/dashboardV4Client';
import { ApiError } from '../../api/dashboardV4/apiError';
import { dashboardV4Keys } from '../../api/queryKeys';
import { mapV4Home } from '../../mappers/dashboardV4';
import type { V4HomeViewModel } from '../../mappers/dashboardV4';

export function useDashboardHomeQuery() {
  return useQuery<DashboardV4HomeDto, ApiError, V4HomeViewModel>({
    queryKey: dashboardV4Keys.home(),
    queryFn: () => dashboardV4Client.getHome(),
    select: mapV4Home,
  });
}
