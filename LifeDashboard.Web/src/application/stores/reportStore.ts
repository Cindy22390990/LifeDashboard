import axios from 'axios';
import { create } from 'zustand';
import type { MonthlyReport } from '../../domain/report';
import { ReportService } from '../../infrastructure/api/ReportService';

interface ApiError {
  message?: string;
}

interface ReportState {
  report: MonthlyReport | null;
  isLoading: boolean;
  error: string | null;
  fetchMonthlyReport: (year: number, month: number) => Promise<void>;
}

function getErrorMessage(error: unknown): string {
  if (!axios.isAxiosError<ApiError>(error)) {
    return '載入月報時發生未預期錯誤。';
  }

  if (error.response?.status === 400) {
    return error.response.data?.message ?? '請選擇有效的年份與月份。';
  }

  if (error.response?.status === 401) {
    return '登入已逾期，請重新登入。';
  }

  if (error.response?.status === 500) {
    return '系統發生錯誤，請稍後再試。';
  }

  return error.response?.data?.message ?? '無法載入月報，請稍後再試。';
}

export const useReportStore = create<ReportState>((set) => ({
  report: null,
  isLoading: false,
  error: null,

  fetchMonthlyReport: async (year, month) => {
    set({ isLoading: true, error: null });

    try {
      const report = await ReportService.getMonthly(year, month);
      set({ report, isLoading: false });
    } catch (error) {
      set({ isLoading: false, error: getErrorMessage(error) });
      throw error;
    }
  },
}));
