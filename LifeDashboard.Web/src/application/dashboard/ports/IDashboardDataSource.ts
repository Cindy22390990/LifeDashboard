import type { DashboardViewModel } from '../models/DashboardViewModel';
import type {
  CalendarDayDetailViewModel,
  CalendarMonthViewModel,
} from '../models/DashboardViewModel';
import type { DashboardFetchOptions } from './DashboardFetchOptions';

export interface IDashboardDataSource {
  getDashboardSummary(
    options?: DashboardFetchOptions,
  ): Promise<DashboardViewModel>;
  getCalendarDayDetail(
    date: string,
    options?: DashboardFetchOptions,
  ): Promise<CalendarDayDetailViewModel>;
  getCalendarMonthSummary(
    year: number,
    month: number,
    options?: DashboardFetchOptions,
  ): Promise<CalendarMonthViewModel>;
}
