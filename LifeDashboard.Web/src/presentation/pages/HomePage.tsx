import { useEffect } from 'react';
import { BottomNavigation } from '../components/dashboard-v4/common/BottomNavigation';
import { DashboardV4Navigation } from '../components/dashboard-v4/common/DashboardV4Navigation';
import { DailyDecisionHero } from '../components/dashboard-v4/home/DailyDecisionHero';
import { MonthlyProgressCard } from '../components/dashboard-v4/home/MonthlyProgressCard';
import { TodayTransactionList } from '../components/dashboard-v4/home/TodayTransactionList';
import { ApiError } from '../../api/dashboardV4/apiError';
import { useDashboardHomeQuery } from '../../hooks/dashboardV4/useDashboardHomeQuery';
import { useDashboardSummaryStore } from '../../application/stores/dashboardSummaryStore';
import { DashboardSummaryCard } from '../components/dashboard-v4/home/DashboardSummaryCard';
import { useTranslation } from 'react-i18next';
import './HomePage.css';

export default function HomePage() {
  const { t } = useTranslation();
  const { data, error, isError, isLoading, isFetching, refetch } =
    useDashboardHomeQuery();
  const {
    summary,
    loading: summaryLoading,
    error: summaryError,
    fetchDashboardSummary,
  } = useDashboardSummaryStore();

  useEffect(() => {
    void fetchDashboardSummary().catch(() => undefined);
  }, [fetchDashboardSummary]);

  return (
    <main className="v4-home" aria-busy={isLoading}>
      <DashboardV4Navigation />
      <header className="v4-home__header">
        <h1>{t('dashboard.title')}</h1>
        <p>{t('dashboard.summary')}</p>
      </header>
      {isLoading ? (
        <section
          aria-label={t('dashboard.loading')}
          className="v4-home__loading"
          role="status"
        >
          <span className="v4-home__skeleton v4-home__skeleton--hero" />
          <span className="v4-home__skeleton" />
          <span className="v4-home__skeleton" />
        </section>
      ) : isError ? (
        <section className="v4-home__error" role="alert">
          <h2>{t('dashboard.loadError')}</h2>
          <p>
            {error instanceof ApiError
              ? error.detail
              : t('dashboard.loadError')}
          </p>
          <button
            disabled={isFetching}
            onClick={() => void refetch()}
            type="button"
          >
            {t('common.retry')}
          </button>
        </section>
      ) : data ? (
        <>
          <DailyDecisionHero viewModel={data} />
          <DashboardSummaryCard
            viewModel={summary ?? {
              totalAssets: 0,
              monthlyIncome: 0,
              monthlyExpense: 0,
            }}
            isLoading={summaryLoading}
            error={summaryError}
            isFetching={summaryLoading}
            onRetry={() => void fetchDashboardSummary().catch(() => undefined)}
          />
          <MonthlyProgressCard viewModel={data.monthlyProgress} />
          <TodayTransactionList
            viewModel={{ todayTransactions: data.todayTransactions }}
          />
        </>
      ) : null}
      <BottomNavigation />
    </main>
  );
}
