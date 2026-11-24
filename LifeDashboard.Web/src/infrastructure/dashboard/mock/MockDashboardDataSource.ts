import type { IDashboardDataSource } from '../../../application/dashboard/ports/IDashboardDataSource';
import { mapDashboardData } from '../mappers/dashboardMapper';
import { mockDashboardData } from './mockDashboardData';

export const mockDashboardDataSource: IDashboardDataSource = {
  async getDashboardSummary() {
    return mapDashboardData(mockDashboardData);
  },
  async getCalendarDayDetail() {
    throw new Error('Calendar read is not available from the mock dashboard data source.');
  },
  async getCalendarMonthSummary() {
    throw new Error('Calendar read is not available from the mock dashboard data source.');
  },
};
