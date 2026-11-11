import type { DashboardSummaryDto } from '../../domain/dashboardSummary';
import http from './http';

export const DashboardSummaryService = {
  getDashboardSummary: (currentDate: string): Promise<DashboardSummaryDto> =>
    http.get<DashboardSummaryDto>('/dashboard/summary', {
      params: { currentDate },
    }),
};