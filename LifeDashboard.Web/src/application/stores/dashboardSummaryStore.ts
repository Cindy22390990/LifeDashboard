import axios from 'axios';
import { create } from 'zustand';
import type { DashboardSummaryDto } from '../../domain/dashboardSummary';
import { DashboardSummaryService } from '../../infrastructure/api/DashboardSummaryService';

interface ApiError {
  message?: string;
}

interface DashboardSummaryState {
  summary: DashboardSummaryDto | null;
  loading: boolean;
  error: string | null;
  fetchDashboardSummary: (currentDate: string) => Promise<void>;
}

function getErrorMessage(error: unknown): string {
  if (!axios.isAxiosError<ApiError>(error)) {
    return '載入預算雷達時發生未預期錯誤。';
  }

  if (error.response?.status === 400) {
    return error.response.data?.message ?? '請提供有效的查詢日期。';
  }

  if (error.response?.status === 404) {
    return error.response.data?.message ?? '尚未完成系統設定。';
  }

  if (error.response?.status === 500) {
    return '系統發生錯誤，請稍後再試。';
  }

  return error.response?.data?.message ?? '無法載入預算雷達，請稍後再試。';
}

export const useDashboardSummaryStore = create<DashboardSummaryState>((set) => ({
  summary: null,
  loading: false,
  error: null,

  fetchDashboardSummary: async (currentDate) => {
    set({ loading: true, error: null });

    try {
      const summary = await DashboardSummaryService.getDashboardSummary(currentDate);
      set({ summary, loading: false });
    } catch (error) {
      set({ loading: false, error: getErrorMessage(error) });
      throw error;
    }
  },
}));