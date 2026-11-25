import type { DashboardSummaryResponse } from '../dashboard/api/DashboardV3SummaryResponse';
import type { DashboardFetchOptions } from '../../application/dashboard/ports/DashboardFetchOptions';
import http from './http';

export const DashboardV3SummaryService = {
  getDashboardSummary(
    options?: DashboardFetchOptions,
  ): Promise<DashboardSummaryResponse> {
    return http.get<DashboardSummaryResponse>(
      '/dashboard/v3/summary',
      { signal: options?.signal },
    );
  },
};
