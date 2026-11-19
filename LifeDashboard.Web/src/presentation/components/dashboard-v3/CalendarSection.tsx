import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMobileViewport } from '../../hooks/useMobileViewport';
import { usePrivacyDisplayStore } from '../../stores/privacyDisplayStore';
import DayDetailPanel from './DayDetailPanel';
import MonthlyCalendar, { type CalendarViewMode } from './MonthlyCalendar';
import { mockDashboardData } from './mockDashboardData';
import './CalendarSection.css';

export default function CalendarSection() {
  const { t, i18n } = useTranslation();
  const locale = i18n.resolvedLanguage ?? i18n.language;
  const isAmountHidden = usePrivacyDisplayStore((state) => state.isAmountHidden);
  const isMobile = useMobileViewport();
  const [selectedDate, setSelectedDate] = useState<string>(mockDashboardData.currentDate);
  const [isMonthExpanded, setIsMonthExpanded] = useState(!isMobile);
  const [isMobilePanelOpen, setIsMobilePanelOpen] = useState(false);
  const viewMode: CalendarViewMode = isMobile && !isMonthExpanded ? 'week' : 'month';
  const selectedTransactions = mockDashboardData.transactions.filter(
    (transaction) => transaction.date === selectedDate,
  );
  const total = Object.entries(mockDashboardData.dailyTotals)
    .find(([date]) => date === selectedDate)?.[1]
    ?? { amount: 0, currency: 'TWD' };

  useEffect(() => {
    setIsMonthExpanded(!isMobile);
    setIsMobilePanelOpen(false);
  }, [isMobile]);

  const handleSelectDate = (date: string) => {
    setSelectedDate(date);
    if (isMobile) {
      setIsMobilePanelOpen(true);
    }
  };

  const dayDetailPanel = (
    <DayDetailPanel
      date={selectedDate}
      isMobileSheet={isMobile}
      isAmountHidden={isAmountHidden}
      locale={locale}
      onClose={() => setIsMobilePanelOpen(false)}
      total={total}
      transactions={selectedTransactions}
    />
  );

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
        <MonthlyCalendar
          currentDate={mockDashboardData.currentDate}
          isMobile={isMobile}
          onExpandMonth={() => setIsMonthExpanded((expanded) => !expanded)}
          onSelectDate={handleSelectDate}
          selectedDate={selectedDate}
          locale={locale}
          viewMode={viewMode}
        />
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
