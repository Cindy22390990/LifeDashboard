import axios from 'axios';

export type ApiErrorFields = Record<string, string[]>;

export class ApiError extends Error {
  readonly status?: number;
  readonly type: string;
  readonly title: string;
  readonly detail: string;
  readonly errors?: ApiErrorFields;

  constructor(
    fields: {
      status?: number;
      type: string;
      title: string;
      detail: string;
      errors?: ApiErrorFields;
    },
  ) {
    super(fields.detail || fields.title);
    this.name = 'ApiError';
    this.status = fields.status;
    this.type = fields.type;
    this.title = fields.title;
    this.detail = fields.detail;
    this.errors = fields.errors;
  }
}

interface ProblemDetails {
  status?: unknown;
  type?: unknown;
  title?: unknown;
  detail?: unknown;
  errors?: unknown;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function parseErrors(value: unknown): ApiErrorFields | undefined {
  if (!isRecord(value)) {
    return undefined;
  }

  const entries = Object.entries(value)
    .map(([key, messages]) => [
      key,
      Array.isArray(messages)
        ? messages.filter((message): message is string => typeof message === 'string')
        : [],
    ] as const)
    .filter(([, messages]) => messages.length > 0);

  return entries.length > 0 ? Object.fromEntries(entries) : undefined;
}

export function parseProblemDetails(
  payload: unknown,
  fallbackStatus?: number,
): ApiError {
  const problem: ProblemDetails = isRecord(payload) ? payload : {};
  const status = typeof problem.status === 'number'
    ? problem.status
    : fallbackStatus;
  const type = typeof problem.type === 'string' ? problem.type : 'about:blank';
  const title = typeof problem.title === 'string'
    ? problem.title
    : 'Request Failed';
  const detail = typeof problem.detail === 'string'
    ? problem.detail
    : 'The request could not be completed.';

  return new ApiError({
    status,
    type,
    title,
    detail,
    errors: parseErrors(problem.errors),
  });
}

export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) {
    return error;
  }

  if (axios.isAxiosError(error)) {
    if (error.response) {
      return parseProblemDetails(error.response.data, error.response.status);
    }

    return new ApiError({
      type: 'about:blank',
      title: 'Network Error',
      detail: 'The server could not be reached. Please try again.',
    });
  }

  return parseProblemDetails(error);
}
