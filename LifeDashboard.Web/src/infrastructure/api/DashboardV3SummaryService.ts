import type { DashboardSummaryResponse } from '../dashboard/api/DashboardV3SummaryResponse';
import http from './http';

export const DashboardV3SummaryService = {
  getDashboardSummary(): Promise<DashboardSummaryResponse> {
    return http.get<DashboardSummaryResponse>('/dashboard/v3/summary');
  },
};
