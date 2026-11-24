import type { DashboardFetchOptions } from '../../application/dashboard/ports/DashboardFetchOptions';
import type { DashboardCalendarDayResponse } from '../dashboard/api/DashboardCalendarDayResponse';
import type { DashboardCalendarMonthResponse } from '../dashboard/api/DashboardCalendarMonthResponse';
import http from './http';

export const DashboardCalendarService = {
  getDayDetail(
    date: string,
    options?: DashboardFetchOptions,
  ): Promise<DashboardCalendarDayResponse> {
    return http.get<DashboardCalendarDayResponse>(
      '/dashboard/v3/calendar/day',
      {
        params: { date },
        signal: options?.signal,
      },
    );
  },
  getMonthSummary(
    year: number,
    month: number,
    options?: DashboardFetchOptions,
  ): Promise<DashboardCalendarMonthResponse> {
    return http.get<DashboardCalendarMonthResponse>(
      '/dashboard/v3/calendar',
      {
        params: { year, month: String(month).padStart(2, '0') },
        signal: options?.signal,
      },
    );
  },
};
