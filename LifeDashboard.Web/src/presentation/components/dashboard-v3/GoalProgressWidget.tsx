import { formatCurrency, formatDate } from '../../../i18n/formatters';
import type { GoalProgressViewModel } from '../../../application/dashboard/models/DashboardViewModel';
import type { SectionState } from '../../../application/dashboard/models/SectionState';
import { useTranslation } from 'react-i18next';
import SectionStateMessage from './SectionStateMessage';
import './GoalProgressWidget.css';

interface GoalProgressWidgetProps {
  state: SectionState<GoalProgressViewModel[]>;
}

export default function GoalProgressWidget({ state }: GoalProgressWidgetProps) {
  const { t, i18n } = useTranslation();
  const locale = i18n.resolvedLanguage ?? i18n.language;

  return (
    <section className="v3-widget v3-goal-widget" aria-labelledby="v3-goal-title">
      <div className="v3-widget__heading">
        <div>
          <p className="v3-section-eyebrow">{t('goals.eyebrow')}</p>
          <h2 id="v3-goal-title">{t('goals.title')}</h2>
        </div>
        {state.status === 'ready' && (
          <span className="v3-goal-widget__mock-label">{t('common.mockData')}</span>
        )}
      </div>
      {state.status === 'ready' ? (
        <ul className="v3-goal-widget__list">
          {state.data.map((goal) => (
            <li className="v3-goal-widget__item" key={goal.goalId}>
              <div className="v3-goal-widget__title">
                <span aria-hidden="true">{goal.icon}</span>
                <strong>{goal.name}</strong>
                <span className="v3-goal-widget__percent">{goal.progressPercentage}%</span>
              </div>
              <div className="v3-goal-widget__amounts">
                <span>
                  {t('goals.saved', {
                    saved: formatCurrency(goal.fundedAmount, locale),
                    target: formatCurrency(goal.targetAmount, locale),
                  })}
                </span>
                <span>
                  {t('goals.left', {
                    amount: formatCurrency(goal.remainingAmount, locale),
                  })}
                </span>
              </div>
              <progress
                aria-label={t('goals.progressAria', {
                  name: goal.name,
                  percent: goal.progressPercentage,
                })}
                max={100}
                value={goal.progressPercentage}
              />
              {goal.deadlineDate && (
                <span className="v3-goal-widget__deadline">
                  {t('goals.deadline', {
                    date: formatDate(goal.deadlineDate, locale, { month: 'short', year: 'numeric' }),
                  })}
                </span>
              )}
            </li>
          ))}
        </ul>
      ) : (
        <SectionStateMessage emptyMessage={t('goals.empty')} state={state} />
      )}
    </section>
  );
}
