import type { ApiError } from './ApiError';

export type SectionState<T> =
  | { status: 'ready'; data: T }
  | { status: 'empty' }
  | { status: 'pending' }
  | { status: 'error'; error: ApiError };
