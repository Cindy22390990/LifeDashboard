import type {
  IFixedCommitment,
  IFixedCommitmentPayload,
} from '../../domain/FixedCommitment';
import http from './http';

export const FixedCommitmentService = {
  getAll: (): Promise<IFixedCommitment[]> =>
    http.get<IFixedCommitment[]>('/fixed-commitments'),

  create: (payload: IFixedCommitmentPayload): Promise<IFixedCommitment> =>
    http.post<IFixedCommitment>('/fixed-commitments', payload),

  update: (id: string, payload: IFixedCommitmentPayload): Promise<void> =>
    http.put<void>(`/fixed-commitments/${id}`, payload),

  delete: (id: string): Promise<void> =>
    http.delete<void>(`/fixed-commitments/${id}`),
};
