import type { ICategory, ICategoryPayload } from '../../domain/Category';
import http from './http';

export const CategoryService = {
  getAll: (): Promise<ICategory[]> =>
    http.get<ICategory[]>('/categories'),

  create: (payload: ICategoryPayload): Promise<ICategory> =>
    http.post<ICategory>('/categories', payload),

  update: (id: string, payload: ICategoryPayload): Promise<void> =>
    http.put<void>(`/categories/${id}`, payload),

  delete: (id: string): Promise<void> =>
    http.delete<void>(`/categories/${id}`),
};
