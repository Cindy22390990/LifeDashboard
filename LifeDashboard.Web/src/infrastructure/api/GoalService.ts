import type {
  IGoal,
  IGoalDepositRequest,
  IGoalPayload,
} from '../../domain/Goal';
import http from './http';

export const GoalService = {
  getAll: (): Promise<IGoal[]> =>
    http.get<IGoal[]>('/goals'),

  deposit: (id: number, request: IGoalDepositRequest): Promise<IGoal> =>
    http.post<IGoal>(`/goals/${id}/deposit`, request),

  create: (payload: IGoalPayload): Promise<IGoal> =>
    http.post<IGoal>('/goals', payload),

  update: (id: number, payload: IGoalPayload): Promise<void> =>
    http.put<void>(`/goals/${id}`, payload),

  delete: (id: number): Promise<void> =>
    http.delete<void>(`/goals/${id}`),
};
