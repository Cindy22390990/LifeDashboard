import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import AccountSummaryWidget from '../components/dashboard-v3/AccountSummaryWidget';
import BudgetRadarWidget from '../components/dashboard-v3/BudgetRadarWidget';
import CalendarSection from '../components/dashboard-v3/CalendarSection';
import GoalProgressWidget from '../components/dashboard-v3/GoalProgressWidget';
import HeroStats from '../components/dashboard-v3/HeroStats';
import NavigationDrawer from '../components/dashboard-v3/NavigationDrawer';
import RecentTransactionsWidget from '../components/dashboard-v3/RecentTransactionsWidget';
import ReportsPlaceholder from '../components/dashboard-v3/ReportsPlaceholder';
import { mockDashboardData } from '../components/dashboard-v3/mockDashboardData';
import { formatDate } from '../../i18n/formatters';
import './DashboardV3Page.css';

export default function DashboardV3Page() {
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const { t, i18n } = useTranslation();
  const locale = i18n.resolvedLanguage ?? i18n.language;

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
          <time dateTime={mockDashboardData.currentDate}>
            {formatDate(mockDashboardData.currentDate, locale, {
              month: 'short',
              day: 'numeric',
              year: 'numeric',
            })}
          </time>
          <span className="dashboard-v3__user-avatar" aria-label="Cindy">C</span>
        </div>
      </header>

      <main className="dashboard-v3__main">
        <div className="dashboard-v3__welcome">
          <div>
            <p className="v3-section-eyebrow">
              {formatDate(mockDashboardData.currentDate, locale, {
                weekday: 'long',
                month: 'long',
                day: 'numeric',
              })}
            </p>
            <h1>{t('dashboard.greeting', { name: 'Cindy' })}</h1>
            <p>{t('dashboard.summary')}</p>
          </div>
        </div>

        <HeroStats />
        <CalendarSection />

        <div className="dashboard-v3__widget-grid">
          <BudgetRadarWidget />
          <GoalProgressWidget />
        </div>

        <div className="dashboard-v3__summary-grid">
          <RecentTransactionsWidget />
          <AccountSummaryWidget />
        </div>

        <ReportsPlaceholder />
      </main>

      <NavigationDrawer
        isOpen={isDrawerOpen}
        onClose={() => setIsDrawerOpen(false)}
        onReportsClick={handleReportsClick}
      />
    </div>
  );
}
