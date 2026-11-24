import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { IDashboardDataSource } from '../../../application/dashboard/ports/IDashboardDataSource';
import { useDashboardCalendar } from '../../../application/dashboard/useDashboardCalendar';
import { useMobileViewport } from '../../hooks/useMobileViewport';
import DayDetailPanel from './DayDetailPanel';
import MonthlyCalendar, { type CalendarViewMode } from './MonthlyCalendar';
import './CalendarSection.css';

interface CalendarSectionProps {
  dataSource: IDashboardDataSource;
  initialDate: string;
}

export default function CalendarSection({
  dataSource,
  initialDate,
}: CalendarSectionProps) {
  const { t, i18n } = useTranslation();
  const locale = i18n.resolvedLanguage ?? i18n.language;
  const isMobile = useMobileViewport();
  const [isMonthExpanded, setIsMonthExpanded] = useState(!isMobile);
  const [isMobilePanelOpen, setIsMobilePanelOpen] = useState(false);
  const calendar = useDashboardCalendar(dataSource, initialDate);
  const viewMode: CalendarViewMode = isMobile && !isMonthExpanded ? 'week' : 'month';

  useEffect(() => {
    setIsMonthExpanded(!isMobile);
    setIsMobilePanelOpen(false);
  }, [isMobile]);

  const handleSelectDate = (date: string) => {
    calendar.selectDate(date);
    if (isMobile) {
      setIsMobilePanelOpen(true);
    }
  };

  const dayDetailPanel = calendar.dayDetail.loading ? (
    <div
      aria-label={t('calendar.dayLoading')}
      aria-busy="true"
      className="v3-calendar-section__loading v3-calendar-section__loading--day"
      role="status"
    >
      <span />
      <span />
      <span />
    </div>
  ) : calendar.dayDetail.error ? (
    <div className="v3-calendar-section__error" role="alert">
      <p>{t('calendar.loadError')}</p>
      <button className="v3-button v3-button--primary" onClick={calendar.retryDay} type="button">
        {t('common.retry')}
      </button>
    </div>
  ) : calendar.dayDetail.data ? (
    <DayDetailPanel
      isMobileSheet={isMobile}
      locale={locale}
      onClose={() => setIsMobilePanelOpen(false)}
      viewModel={calendar.dayDetail.data}
    />
  ) : null;

  return (
    <section className="v3-calendar-section" aria-labelledby="v3-calendar-title">
      <div className="v3-calendar-section__heading">
        <div>
          <p className="v3-section-eyebrow">{t('calendar.activity')}</p>
          <h2 id="v3-calendar-title">{t('calendar.title')}</h2>
        </div>
        <span className="v3-calendar-section__legend">
          <span className="v3-calendar-section__legend-income">
            ↑ {t('calendar.income')}
          </span>
          <span className="v3-calendar-section__legend-expense">
            ↓ {t('calendar.expense')}
          </span>
        </span>
      </div>
      <div className="v3-calendar-section__content">
        {calendar.monthSummary.loading ? (
          <div
            aria-label={t('calendar.monthLoading')}
            aria-busy="true"
            className="v3-calendar-section__loading"
            role="status"
          >
            <span />
            <span />
            <span />
          </div>
        ) : calendar.monthSummary.error ? (
          <div className="v3-calendar-section__error" role="alert">
            <p>{t('calendar.loadError')}</p>
            <button className="v3-button v3-button--primary" onClick={calendar.retryMonth} type="button">
              {t('common.retry')}
            </button>
          </div>
        ) : calendar.monthSummary.data && calendar.viewedMonth ? (
          <MonthlyCalendar
            viewModel={calendar.monthSummary.data}
            isMobile={isMobile}
            onExpandMonth={() => setIsMonthExpanded((expanded) => !expanded)}
            onNavigateMonth={calendar.navigateMonth}
            onSelectDate={handleSelectDate}
            selectedDate={calendar.selectedDate ?? initialDate}
            locale={locale}
            viewMode={viewMode}
          />
        ) : null}
        {!isMobile && dayDetailPanel}
      </div>
      {isMobile && isMobilePanelOpen && (
        <>
          <button
            aria-label={t('calendar.closeDetails')}
            className="v3-day-detail-backdrop"
            onClick={() => setIsMobilePanelOpen(false)}
            type="button"
          />
          {dayDetailPanel}
        </>
      )}
    </section>
  );
}
