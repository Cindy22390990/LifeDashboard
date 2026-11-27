import axios from 'axios';
import { create } from 'zustand';
import { DashboardSummaryService } from '../../infrastructure/api/DashboardSummaryService';
import { mapV4DashboardSummary } from '../../mappers/dashboardV4';
import type { V4DashboardSummaryViewModel } from '../../mappers/dashboardV4';

interface ApiError {
  message?: string;
}

interface DashboardSummaryState {
  summary: V4DashboardSummaryViewModel | null;
  loading: boolean;
  error: string | null;
  fetchDashboardSummary: () => Promise<void>;
}

function getErrorMessage(error: unknown): string {
  if (!axios.isAxiosError<ApiError>(error)) {
    return '載入儀表板資料時發生未預期錯誤。';
  }

  if (error.response?.status === 400) {
    return error.response.data?.message ?? '無法載入儀表板摘要。';
  }

  if (error.response?.status === 404) {
    return error.response.data?.message ?? '尚未完成系統設定。';
  }

  if (error.response?.status === 500) {
    return '系統發生錯誤，請稍後再試。';
  }

  return error.response?.data?.message ?? '無法載入儀表板資料，請稍後再試。';
}

export const useDashboardSummaryStore = create<DashboardSummaryState>((set) => ({
  summary: null,
  loading: false,
  error: null,

  fetchDashboardSummary: async () => {
    set({ loading: true, error: null });

    try {
      const summary = await DashboardSummaryService.getDashboardSummary();
      set({ summary: mapV4DashboardSummary(summary), loading: false });
    } catch (error) {
      set({ loading: false, error: getErrorMessage(error) });
      throw error;
    }
  },
}));