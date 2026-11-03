import axios from 'axios';
import { create } from 'zustand';
import type {
  IFixedCommitment,
  IFixedCommitmentPayload,
} from '../../domain/FixedCommitment';
import { FixedCommitmentService } from '../../infrastructure/api/FixedCommitmentService';

interface ApiError {
  message?: string;
  Message?: string;
}

interface FixedCommitmentState {
  commitments: IFixedCommitment[];
  isLoading: boolean;
  error: string | null;
  fetchCommitments: () => Promise<void>;
  addCommitment: (payload: IFixedCommitmentPayload) => Promise<void>;
  updateCommitment: (id: string, payload: IFixedCommitmentPayload) => Promise<void>;
  removeCommitment: (id: string) => Promise<void>;
}

function getErrorMessage(error: unknown): string {
  if (!axios.isAxiosError<ApiError>(error)) {
    return '固定責任操作時發生未預期錯誤。';
  }

  return error.response?.data?.message
    ?? error.response?.data?.Message
    ?? '無法完成固定責任操作，請稍後再試。';
}

export const useFixedCommitmentStore = create<FixedCommitmentState>((set, get) => ({
  commitments: [],
  isLoading: false,
  error: null,

  fetchCommitments: async () => {
    set({ isLoading: true, error: null });
    try {
      const commitments = await FixedCommitmentService.getAll();
      set({ commitments, isLoading: false });
    } catch (error) {
      set({ isLoading: false, error: getErrorMessage(error) });
      throw error;
    }
  },

  addCommitment: async (payload) => {
    set({ error: null });
    try {
      await FixedCommitmentService.create(payload);
      await get().fetchCommitments();
    } catch (error) {
      set({ isLoading: false, error: getErrorMessage(error) });
      throw error;
    }
  },

  updateCommitment: async (id, payload) => {
    set({ error: null });
    try {
      await FixedCommitmentService.update(id, payload);
      await get().fetchCommitments();
    } catch (error) {
      set({ isLoading: false, error: getErrorMessage(error) });
      throw error;
    }
  },

  removeCommitment: async (id) => {
    set({ error: null });
    try {
      await FixedCommitmentService.delete(id);
      await get().fetchCommitments();
    } catch (error) {
      set({ isLoading: false, error: getErrorMessage(error) });
      throw error;
    }
  },
}));
