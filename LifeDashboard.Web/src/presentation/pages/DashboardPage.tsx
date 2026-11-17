import { useEffect, useState } from 'react';
import { useDashboardSummaryStore } from '../../application/stores/dashboardSummaryStore';
import FinancialOverview from '../components/dashboard/FinancialOverview';
import BudgetRadarSummary from '../components/dashboard/BudgetRadarSummary';
import GoalSummary from '../components/dashboard/GoalSummary';
import RecentActivity from '../components/dashboard/RecentActivity';
import AccountDetails from '../components/dashboard/AccountDetails';
import QuickAddEntry from '../components/dashboard/QuickAddEntry';
import './DashboardPage.css';

function getLocalDateString(date: Date): string {
  const year = String(date.getFullYear()).padStart(4, '0');
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');

  return `${year}-${month}-${day}`;
}

interface DashboardPageProps {
  onNavigateToCategories?: () => void;
}

export default function DashboardPage({ onNavigateToCategories }: DashboardPageProps) {
  const [currentDate] = useState(() => getLocalDateString(new Date()));
  const {
    summary,
    loading,
    error,
    fetchDashboardSummary,
  } = useDashboardSummaryStore();
  const displayDate = new Date(`${currentDate}T00:00:00`).toLocaleDateString('zh-TW', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    weekday: 'long',
  });

  useEffect(() => {
    void fetchDashboardSummary(currentDate).catch(() => undefined);
  }, [currentDate, fetchDashboardSummary]);

  return (
    <div className="dashboard-page">
      <header className="dashboard-header">
        <div>
          <p className="dashboard-header__eyebrow">LIFE DASHBOARD</p>
          <h1>生活財務總覽</h1>
          <p className="dashboard-header__subtitle">財務概況、預算與目標，一頁掌握。</p>
        </div>
        <div className="dashboard-header__actions">
          <time className="dashboard-header__date" dateTime={currentDate}>{displayDate}</time>
          <QuickAddEntry onOpenTransactionEntry={onNavigateToCategories} />
        </div>
      </header>

      {loading && <p className="dashboard-feedback">載入中…</p>}
      {!loading && error && <p className="dashboard-feedback" role="alert">{error}</p>}

      {!loading && !error && summary && (
        <>
          <div className="dashboard-content">
            <div className="dashboard-primary-grid" aria-label="財務概況">
              <FinancialOverview
                totalAssets={summary.totalAssets}
                monthlyIncome={summary.monthlyIncome}
                monthlyExpense={summary.monthlyExpense}
                todayRemaining={summary.budget.todayRemaining}
                currentDate={currentDate}
              />
              <BudgetRadarSummary radarState={summary.budget} />
              <GoalSummary goals={summary.goals} />
            </div>

            <div className="dashboard-secondary-grid">
              <RecentActivity transactions={summary.recentTransactions} />
              <AccountDetails accounts={summary.accounts} />
            </div>
          </div>
        </>
      )}
    </div>
  );
}
