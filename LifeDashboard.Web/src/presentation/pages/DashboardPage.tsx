import { useEffect } from 'react';
import { useDashboardSummaryStore } from '../../application/stores/dashboardSummaryStore';
import { DashboardSummaryCard } from '../components/dashboard-v4/home/DashboardSummaryCard';
import './DashboardPage.css';

export default function DashboardPage() {
  const {
    summary,
    loading,
    error,
    fetchDashboardSummary,
  } = useDashboardSummaryStore();

  useEffect(() => {
    void fetchDashboardSummary().catch(() => undefined);
  }, [fetchDashboardSummary]);

  return (
    <main className="dashboard-page">
      <header className="dashboard-header">
        <div>
          <p className="dashboard-header__eyebrow">LIFE DASHBOARD</p>
          <h1>生活財務總覽</h1>
          <p className="dashboard-header__subtitle">本月財務摘要。</p>
        </div>
      </header>

      <DashboardSummaryCard
        viewModel={summary ?? {
          totalAssets: 0,
          monthlyIncome: 0,
          monthlyExpense: 0,
        }}
        isLoading={loading}
        error={error}
        isFetching={loading}
        onRetry={() => void fetchDashboardSummary().catch(() => undefined)}
      />
    </main>
  );
}
