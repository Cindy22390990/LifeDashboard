import { useEffect, useState } from 'react';
import { useCategoryStore } from '../../application/stores/categoryStore';
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
  onNavigateToTransactions?: () => void;
}

export default function DashboardPage({ onNavigateToTransactions }: DashboardPageProps) {
  const [currentDate] = useState(() => getLocalDateString(new Date()));
  const { categories, error: categoryError, fetchCategories } = useCategoryStore();
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

  useEffect(() => {
    if (categories.length === 0) {
      void fetchCategories().catch(() => undefined);
    }
  }, [categories.length, fetchCategories]);

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
          <QuickAddEntry onOpenTransactionEntry={onNavigateToTransactions} />
        </div>
      </header>

      {loading && <p className="dashboard-feedback">載入中…</p>}
      {!loading && error && <p className="dashboard-feedback" role="alert">{error}</p>}
      {!loading && summary && categoryError && (
        <p className="dashboard-feedback" role="status">{categoryError}</p>
      )}

      {!loading && !error && summary && (
        <div className="dashboard-content">
          <div className="dashboard-content__financial">
            <FinancialOverview
              totalAssets={summary.totalAssets}
              monthlyIncome={summary.monthlyIncome}
              monthlyExpense={summary.monthlyExpense}
              availableMoney={summary.budget.remainingLivingBudget}
              currentDate={currentDate}
            />
          </div>
          <div className="dashboard-content__budget">
            <BudgetRadarSummary radarState={summary.budget} />
          </div>
          <div className="dashboard-content__goals">
            <GoalSummary />
          </div>
          <div className="dashboard-content__transactions">
            <RecentActivity
              categories={categories}
              transactions={summary.recentTransactions}
            />
          </div>
          <div className="dashboard-content__accounts">
            <AccountDetails accounts={summary.accounts} />
          </div>
        </div>
      )}
    </div>
  );
}
