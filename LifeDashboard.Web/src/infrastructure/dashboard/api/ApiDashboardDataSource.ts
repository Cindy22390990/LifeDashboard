import type { IDashboardDataSource } from '../../../application/dashboard/ports/IDashboardDataSource';
import { DashboardCalendarService } from '../../api/DashboardCalendarService';
import { DashboardV3SummaryService } from '../../api/DashboardV3SummaryService';
import { mapDashboardCalendarDay, mapDashboardCalendarMonth } from '../mappers/calendarMapper';
import { mapDashboardV3Api } from '../mappers/dashboardV3ApiMapper';

export const apiDashboardDataSource: IDashboardDataSource = {
  async getDashboardSummary(options) {
    const summary = await DashboardV3SummaryService.getDashboardSummary(options);
    return mapDashboardV3Api(summary);
  },
  async getCalendarDayDetail(date, options) {
    const response = await DashboardCalendarService.getDayDetail(date, options);
    return mapDashboardCalendarDay(response);
  },
  async getCalendarMonthSummary(year, month, options) {
    const response = await DashboardCalendarService.getMonthSummary(year, month, options);
    return mapDashboardCalendarMonth(response);
  },
};
