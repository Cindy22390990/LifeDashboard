import { mockDashboardData } from './mockDashboardData';
import { formatSensitiveMoney } from '../../../i18n/formatters';
import { useTranslation } from 'react-i18next';
import { usePrivacyDisplayStore } from '../../stores/privacyDisplayStore';
import './BudgetRadarWidget.css';

export default function BudgetRadarWidget() {
  const { t, i18n } = useTranslation();
  const locale = i18n.resolvedLanguage ?? i18n.language;
  const isAmountHidden = usePrivacyDisplayStore((state) => state.isAmountHidden);
  const { budget } = mockDashboardData;

  return (
    <section className="v3-widget v3-budget-widget" aria-labelledby="v3-budget-title">
      <div className="v3-widget__heading">
        <div>
          <p className="v3-section-eyebrow">{t('budget.eyebrow')}</p>
          <h2 id="v3-budget-title">{t('budget.title')}</h2>
        </div>
        <span className="v3-budget-widget__status">
          <span aria-hidden="true">●</span>
          {t(budget.statusKey)}
        </span>
      </div>
      <div className="v3-budget-widget__main">
        <div className="v3-budget-widget__value">
          <span>{t('budget.leftToSpend')}</span>
          <strong>
            {formatSensitiveMoney(budget.remainingAmount, locale, isAmountHidden)}
          </strong>
          <small>{t(budget.periodKey)}</small>
        </div>
        <div className="v3-budget-widget__used">
          <div className="v3-budget-widget__used-label">
            <span>{t('budget.used')}</span>
            <strong>{budget.usedPercent}%</strong>
          </div>
          <progress
            aria-label={t('budget.progressAria', { percent: budget.usedPercent })}
            max={100}
            value={budget.usedPercent}
          />
          <small>{t('budget.mockStatus')}</small>
        </div>
      </div>
    </section>
  );
}
