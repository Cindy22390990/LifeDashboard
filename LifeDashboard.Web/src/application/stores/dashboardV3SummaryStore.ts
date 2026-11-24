import axios from 'axios';
import { create } from 'zustand';
import type { DashboardSummaryResponse } from '../../infrastructure/dashboard/api/DashboardV3SummaryResponse';
import { DashboardV3SummaryService } from '../../infrastructure/api/DashboardV3SummaryService';

interface DashboardV3SummaryState {
  summary: DashboardSummaryResponse | null;
  loading: boolean;
  error: string | null;
  fetchDashboardSummary: () => Promise<void>;
}

function getErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    return error.response?.status
      ? '無法載入儀表板資料，請稍後再試。'
      : '無法連線至伺服器。';
  }

  return '載入儀表板資料時發生未預期錯誤。';
}

export const useDashboardV3SummaryStore = create<DashboardV3SummaryState>((set) => ({
  summary: null,
  loading: false,
  error: null,

  fetchDashboardSummary: async () => {
    set({ loading: true, error: null });

    try {
      const summary = await DashboardV3SummaryService.getDashboardSummary();
      set({ summary, loading: false });
    } catch (error) {
      set({
        loading: false,
        error: getErrorMessage(error),
      });
      throw error;
    }
  },
}));
