import { formatCurrency } from '../../../i18n/formatters';
import type { BudgetRadarViewModel } from '../../../application/dashboard/models/DashboardViewModel';
import type { SectionState } from '../../../application/dashboard/models/SectionState';
import { useTranslation } from 'react-i18next';
import SectionStateMessage from './SectionStateMessage';
import './BudgetRadarWidget.css';

interface BudgetRadarWidgetProps {
  state: SectionState<BudgetRadarViewModel>;
}

const statusTranslationKeys = {
  normal: 'budget.safe',
  warning: 'budget.warning',
  exceeded: 'budget.exceeded',
  unavailable: 'budget.unavailable',
} as const;

export default function BudgetRadarWidget({ state }: BudgetRadarWidgetProps) {
  const { t, i18n } = useTranslation();
  const locale = i18n.resolvedLanguage ?? i18n.language;
  return (
    <section className="v3-widget v3-budget-widget" aria-labelledby="v3-budget-title">
      <div className="v3-widget__heading">
        <div>
          <p className="v3-section-eyebrow">{t('budget.eyebrow')}</p>
          <h2 id="v3-budget-title">{t('budget.title')}</h2>
        </div>
        {state.status === 'ready' && (
          <span className="v3-budget-widget__status">
            <span aria-hidden="true">●</span>
            {t(statusTranslationKeys[state.data.healthStatus])}
          </span>
        )}
      </div>
      {state.status === 'ready' ? (
        <div className="v3-budget-widget__main">
          <div className="v3-budget-widget__value">
            <span>{t('budget.leftToSpend')}</span>
            <strong>
              {formatCurrency(state.data.remainingLivingBudget, locale)}
            </strong>
            <small>{t(state.data.periodKey)}</small>
          </div>
          <div className="v3-budget-widget__used">
            <div className="v3-budget-widget__used-label">
              <span>{t('budget.used')}</span>
              <strong>{state.data.usagePercentage}%</strong>
            </div>
            <progress
              aria-label={t('budget.progressAria', { percent: state.data.usagePercentage })}
              max={100}
              value={state.data.usagePercentage}
            />
            <small>{t('budget.mockStatus')}</small>
          </div>
        </div>
      ) : (
        <SectionStateMessage emptyMessage={t('budget.unavailable')} state={state} />
      )}
    </section>
  );
}
