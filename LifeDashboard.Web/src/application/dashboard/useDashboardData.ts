import { useCallback, useEffect, useRef, useState } from 'react';
import type { IDashboardDataSource } from './ports/IDashboardDataSource';
import type { DashboardViewModel } from './models/DashboardViewModel';

interface DashboardDataState {
  data: DashboardViewModel | null;
  isLoading: boolean;
  error: Error | null;
}

function isAbortError(error: unknown): boolean {
  if (typeof error !== 'object' || error === null) {
    return false;
  }

  if (
    'name' in error &&
    (error.name === 'AbortError' || error.name === 'CanceledError')
  ) {
    return true;
  }

  return 'code' in error && error.code === 'ERR_CANCELED';
}

export function useDashboardData(dataSource: IDashboardDataSource) {
  const [state, setState] = useState<DashboardDataState>({
    data: null,
    isLoading: true,
    error: null,
  });
  const activeController = useRef<AbortController | null>(null);
  const requestId = useRef(0);

  const retry = useCallback(async () => {
    activeController.current?.abort();
    const controller = new AbortController();
    activeController.current = controller;
    const currentRequestId = ++requestId.current;

    setState((current) => ({ ...current, isLoading: true, error: null }));

    try {
      const data = await dataSource.getDashboardSummary({
        signal: controller.signal,
      });

      if (controller.signal.aborted || currentRequestId !== requestId.current) {
        return;
      }

      setState({ data, isLoading: false, error: null });
    } catch (error) {
      if (
        controller.signal.aborted ||
        currentRequestId !== requestId.current ||
        isAbortError(error)
      ) {
        return;
      }

      setState({
        data: null,
        isLoading: false,
        error: error instanceof Error ? error : new Error(String(error)),
      });
    } finally {
      if (activeController.current === controller) {
        activeController.current = null;
      }
    }
  }, [dataSource]);

  useEffect(() => {
    void retry();

    return () => {
      requestId.current += 1;
      activeController.current?.abort();
      activeController.current = null;
    };
  }, [retry]);

  return {
    ...state,
    retry,
  };
}
