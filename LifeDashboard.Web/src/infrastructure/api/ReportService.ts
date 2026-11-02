import http from './http';
import type { MonthlyReport } from '../../domain/report';

export const ReportService = {
  getMonthly: (year: number, month: number): Promise<MonthlyReport> =>
    http.get<MonthlyReport>('/reports/monthly', {
      params: { year, month },
    }),
};
