import type { DashboardSummaryDto } from '../../api/dashboard-v4/dto/dashboardSummaryDto';
import http from './http';

export const DashboardSummaryService = {
  getDashboardSummary: (): Promise<DashboardSummaryDto> =>
    http.get<DashboardSummaryDto>('/dashboard/summary'),
};