import { useCallback, useEffect, useState } from 'react';
import type { IDashboardDataSource } from './ports/IDashboardDataSource';
import type { DashboardViewModel } from './models/DashboardViewModel';

interface DashboardDataState {
  data: DashboardViewModel | null;
  isLoading: boolean;
  error: Error | null;
}

export function useDashboardData(dataSource: IDashboardDataSource) {
  const [state, setState] = useState<DashboardDataState>({
    data: null,
    isLoading: true,
    error: null,
  });

  const loadDashboard = useCallback(async () => {
    setState((current) => ({ ...current, isLoading: true, error: null }));
    try {
      const data = await dataSource.getDashboardSummary();
      setState({ data, isLoading: false, error: null });
    } catch (error) {
      setState({
        data: null,
        isLoading: false,
        error: error instanceof Error ? error : new Error(String(error)),
      });
    }
  }, [dataSource]);

  useEffect(() => {
    void loadDashboard();
  }, [loadDashboard]);

  return {
    ...state,
    refetch: loadDashboard,
  };
}
