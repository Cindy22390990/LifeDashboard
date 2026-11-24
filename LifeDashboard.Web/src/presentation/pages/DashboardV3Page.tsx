import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { IDashboardDataSource } from '../../application/dashboard/ports/IDashboardDataSource';
import { useDashboardData } from '../../application/dashboard/useDashboardData';
import AccountSummaryWidget from '../components/dashboard-v3/AccountSummaryWidget';
import BudgetRadarWidget from '../components/dashboard-v3/BudgetRadarWidget';
import CalendarSection from '../components/dashboard-v3/CalendarSection';
import GoalProgressWidget from '../components/dashboard-v3/GoalProgressWidget';
import HeroStats from '../components/dashboard-v3/HeroStats';
import NavigationDrawer from '../components/dashboard-v3/NavigationDrawer';
import RecentTransactionsWidget from '../components/dashboard-v3/RecentTransactionsWidget';
import ReportsPlaceholder from '../components/dashboard-v3/ReportsPlaceholder';
import { formatDate } from '../../i18n/formatters';
import './DashboardV3Page.css';

interface DashboardV3PageProps {
  dataSource: IDashboardDataSource;
}

export default function DashboardV3Page({ dataSource }: DashboardV3PageProps) {
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const { t, i18n } = useTranslation();
  const locale = i18n.resolvedLanguage ?? i18n.language;
  const { data, isLoading, error, refetch } = useDashboardData(dataSource);

  const handleReportsClick = () => {
    setIsDrawerOpen(false);
    document.getElementById('reports-placeholder')?.scrollIntoView({
      behavior: 'smooth',
      block: 'start',
    });
  };

  return (
    <div className="dashboard-v3">
      <header className="dashboard-v3__topbar">
        <div className="dashboard-v3__topbar-start">
          <button
            aria-label={t('dashboard.openNavigation')}
            className="dashboard-v3__menu-trigger"
            onClick={() => setIsDrawerOpen(true)}
            type="button"
          >
            <span />
            <span />
            <span />
          </button>
          <strong>{t('dashboard.title')}</strong>
        </div>
        <div className="dashboard-v3__topbar-end">
          {data && (
            <time dateTime={data.meta.businessDate}>
              {formatDate(data.meta.businessDate, locale, {
                month: 'short',
                day: 'numeric',
                year: 'numeric',
              })}
            </time>
          )}
          <span className="dashboard-v3__user-avatar" aria-label="Cindy">C</span>
        </div>
      </header>

      <main className="dashboard-v3__main">
        {data ? (
          <>
            <div className="dashboard-v3__welcome">
              <div>
                <p className="v3-section-eyebrow">
                  {formatDate(data.meta.businessDate, locale, {
                    weekday: 'long',
                    month: 'long',
                    day: 'numeric',
                  })}
                </p>
                <h1>{t('dashboard.greeting', { name: 'Cindy' })}</h1>
                <p>{t('dashboard.summary')}</p>
              </div>
            </div>

            <HeroStats viewModel={data.heroStats} />
            <CalendarSection
              dataSource={dataSource}
              initialDate={data.meta.businessDate}
            />

            <div className="dashboard-v3__widget-grid">
              <BudgetRadarWidget state={data.budgetRadar} />
              <GoalProgressWidget state={data.goalProgress} />
            </div>

            <div className="dashboard-v3__summary-grid">
              <RecentTransactionsWidget state={data.recentTransactions} />
              <AccountSummaryWidget state={data.accountSummary} />
            </div>

            <ReportsPlaceholder />
          </>
        ) : isLoading ? (
          <div
            aria-label={t('dashboard.loading')}
            aria-busy="true"
            className="dashboard-v3__loading-state"
          >
            <span />
            <span />
            <span />
          </div>
        ) : (
          <section aria-labelledby="dashboard-v3-error-title" className="dashboard-v3__error-state" role="alert">
            <h1 id="dashboard-v3-error-title">{t('dashboard.loadError')}</h1>
            {error && <p>{error.message}</p>}
            <button className="v3-button v3-button--primary" onClick={() => void refetch()} type="button">
              {t('common.retry')}
            </button>
          </section>
        )}
      </main>

      <NavigationDrawer
        isOpen={isDrawerOpen}
        onClose={() => setIsDrawerOpen(false)}
        onReportsClick={handleReportsClick}
      />
    </div>
  );
}
