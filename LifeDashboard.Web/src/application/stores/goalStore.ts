import axios from 'axios';
import { create } from 'zustand';
import type {
  IGoal,
  IGoalDepositRequest,
  IGoalPayload,
} from '../../domain/Goal';
import { GoalService } from '../../infrastructure/api/GoalService';

interface ApiError {
  message?: string;
  Message?: string;
}

interface GoalState {
  goals: IGoal[];
  isLoading: boolean;
  error: string | null;
  fetchGoals: () => Promise<void>;
  depositGoal: (id: number, request: IGoalDepositRequest) => Promise<void>;
  addGoal: (payload: IGoalPayload) => Promise<void>;
  updateGoal: (id: number, payload: IGoalPayload) => Promise<void>;
  removeGoal: (id: number) => Promise<void>;
}

function getErrorMessage(error: unknown): string {
  if (!axios.isAxiosError<ApiError>(error)) {
    return '目標操作時發生未預期錯誤。';
  }

  return error.response?.data?.message
    ?? error.response?.data?.Message
    ?? '無法完成目標操作，請稍後再試。';
}

export const useGoalStore = create<GoalState>((set, get) => ({
  goals: [],
  isLoading: false,
  error: null,

  fetchGoals: async () => {
    set({ isLoading: true, error: null });
    try {
      const goals = await GoalService.getAll();
      set({ goals, isLoading: false });
    } catch (error) {
      set({ isLoading: false, error: getErrorMessage(error) });
      throw error;
    }
  },

  depositGoal: async (id, request) => {
    set({ error: null });
    try {
      await GoalService.deposit(id, request);
      await get().fetchGoals();
    } catch (error) {
      set({ isLoading: false, error: getErrorMessage(error) });
      throw error;
    }
  },

  addGoal: async (payload) => {
    set({ error: null });
    try {
      await GoalService.create(payload);
      await get().fetchGoals();
    } catch (error) {
      set({ isLoading: false, error: getErrorMessage(error) });
      throw error;
    }
  },

  updateGoal: async (id, payload) => {
    set({ error: null });
    try {
      await GoalService.update(id, payload);
      await get().fetchGoals();
    } catch (error) {
      set({ isLoading: false, error: getErrorMessage(error) });
      throw error;
    }
  },

  removeGoal: async (id) => {
    set({ error: null });
    try {
      await GoalService.delete(id);
      await get().fetchGoals();
    } catch (error) {
      set({ isLoading: false, error: getErrorMessage(error) });
      throw error;
    }
  },
}));
