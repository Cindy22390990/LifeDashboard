import { mockDashboardData } from './mockDashboardData';
import { formatDate, formatSensitiveMoney } from '../../../i18n/formatters';
import { useTranslation } from 'react-i18next';
import { usePrivacyDisplayStore } from '../../stores/privacyDisplayStore';
import './GoalProgressWidget.css';

export default function GoalProgressWidget() {
  const { t, i18n } = useTranslation();
  const locale = i18n.resolvedLanguage ?? i18n.language;
  const isAmountHidden = usePrivacyDisplayStore((state) => state.isAmountHidden);

  return (
    <section className="v3-widget v3-goal-widget" aria-labelledby="v3-goal-title">
      <div className="v3-widget__heading">
        <div>
          <p className="v3-section-eyebrow">{t('goals.eyebrow')}</p>
          <h2 id="v3-goal-title">{t('goals.title')}</h2>
        </div>
        <span className="v3-goal-widget__mock-label">{t('common.mockData')}</span>
      </div>
      <ul className="v3-goal-widget__list">
        {mockDashboardData.goals.map((goal) => (
          <li className="v3-goal-widget__item" key={goal.id}>
            <div className="v3-goal-widget__title">
              <span aria-hidden="true">{goal.icon}</span>
              <strong>{goal.name}</strong>
              <span className="v3-goal-widget__percent">{goal.progressPercent}%</span>
            </div>
            <div className="v3-goal-widget__amounts">
              <span>
                {t('goals.saved', {
                  saved: formatSensitiveMoney(goal.savedAmount, locale, isAmountHidden),
                  target: formatSensitiveMoney(goal.targetAmount, locale, isAmountHidden),
                })}
              </span>
              <span>
                {t('goals.left', {
                  amount: formatSensitiveMoney(goal.remainingAmount, locale, isAmountHidden),
                })}
              </span>
            </div>
            <progress
              aria-label={t('goals.progressAria', {
                name: goal.name,
                percent: goal.progressPercent,
              })}
              max={100}
              value={goal.progressPercent}
            />
            <span className="v3-goal-widget__deadline">
              {t('goals.deadline', {
                date: formatDate(goal.deadline, locale, { month: 'short', year: 'numeric' }),
              })}
            </span>
          </li>
        ))}
      </ul>
    </section>
  );
}
