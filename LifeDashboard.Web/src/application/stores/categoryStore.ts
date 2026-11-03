import axios from 'axios';
import { create } from 'zustand';
import type { ICategory, ICategoryPayload } from '../../domain/Category';
import { CategoryService } from '../../infrastructure/api/CategoryService';

interface ApiError {
  message?: string;
  Message?: string;
}

interface CategoryState {
  categories: ICategory[];
  isLoading: boolean;
  error: string | null;
  fetchCategories: () => Promise<void>;
  addCategory: (payload: ICategoryPayload) => Promise<void>;
  updateCategory: (id: string, payload: ICategoryPayload) => Promise<void>;
  removeCategory: (id: string) => Promise<void>;
}

function getErrorMessage(error: unknown): string {
  if (!axios.isAxiosError<ApiError>(error)) {
    return '分類操作時發生未預期錯誤。';
  }

  return error.response?.data?.message
    ?? error.response?.data?.Message
    ?? '無法完成分類操作，請稍後再試。';
}

export const useCategoryStore = create<CategoryState>((set, get) => ({
  categories: [],
  isLoading: false,
  error: null,

  fetchCategories: async () => {
    set({ isLoading: true, error: null });
    try {
      const categories = await CategoryService.getAll();
      set({ categories, isLoading: false });
    } catch (error) {
      set({ isLoading: false, error: getErrorMessage(error) });
      throw error;
    }
  },

  addCategory: async (payload) => {
    set({ error: null });
    try {
      await CategoryService.create(payload);
      await get().fetchCategories();
    } catch (error) {
      set({ error: getErrorMessage(error), isLoading: false });
      throw error;
    }
  },

  updateCategory: async (id, payload) => {
    set({ error: null });
    try {
      await CategoryService.update(id, payload);
      await get().fetchCategories();
    } catch (error) {
      set({ error: getErrorMessage(error), isLoading: false });
      throw error;
    }
  },

  removeCategory: async (id) => {
    set({ error: null });
    try {
      await CategoryService.delete(id);
      await get().fetchCategories();
    } catch (error) {
      set({ error: getErrorMessage(error), isLoading: false });
      throw error;
    }
  },
}));
