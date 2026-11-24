import { formatSensitiveMoney } from '../../../i18n/formatters';
import type { HeroStatsViewModel } from '../../../application/dashboard/models/DashboardViewModel';
import { useTranslation } from 'react-i18next';
import { usePrivacyDisplayStore } from '../../stores/privacyDisplayStore';
import './HeroStats.css';

interface HeroStatsProps {
  viewModel: HeroStatsViewModel;
}

export default function HeroStats({ viewModel }: HeroStatsProps) {
  const { t, i18n } = useTranslation();
  const locale = i18n.resolvedLanguage ?? i18n.language;
  const isAmountHidden = usePrivacyDisplayStore((state) => state.isAmountHidden);
  const toggleVisibility = usePrivacyDisplayStore((state) => state.toggleVisibility);

  const stats = [
    {
      id: 'assets',
      amount: viewModel.totalAssets,
      labelKey: 'dashboard.hero.totalAssets',
      captionKey: 'dashboard.hero.netWorth',
      emphasis: 'primary',
      icon: '◇',
    },
    {
      id: 'spent',
      amount: viewModel.spentToday,
      labelKey: 'dashboard.hero.spentToday',
      captionKey: 'dashboard.hero.today',
      emphasis: 'standard',
      icon: '↓',
    },
    {
      id: 'remaining',
      amount: viewModel.remainingBudget,
      labelKey: 'dashboard.hero.leftToSpend',
      captionKey: 'dashboard.hero.budgetCycle',
      emphasis: 'positive',
      icon: '○',
    },
  ];

  return (
    <section className="v3-hero-stats" aria-label={t('dashboard.hero.ariaLabel')}>
      <div className="v3-hero-stats__toolbar">
        <button
          aria-label={t(isAmountHidden ? 'privacy.showAmounts' : 'privacy.hideAmounts')}
          aria-pressed={isAmountHidden}
          className="v3-hero-stats__privacy-toggle"
          onClick={toggleVisibility}
          type="button"
        >
          <svg aria-hidden="true" fill="none" viewBox="0 0 24 24">
            <path
              d="M2 12s3.6-7 10-7 10 7 10 7-3.6 7-10 7S2 12 2 12Z"
              stroke="currentColor"
              strokeWidth="1.8"
            />
            <circle cx="12" cy="12" r="3" stroke="currentColor" strokeWidth="1.8" />
            {isAmountHidden && (
              <path d="m4 4 16 16" stroke="currentColor" strokeWidth="1.8" />
            )}
          </svg>
        </button>
      </div>
      <div className="v3-hero-stats__grid">
        {stats.map((stat) => (
          <article
            className={`v3-hero-stat v3-hero-stat--${stat.emphasis}`}
            key={stat.id}
          >
            <div className="v3-hero-stat__heading">
              <span className="v3-hero-stat__icon" aria-hidden="true">{stat.icon}</span>
              <span>{t(stat.labelKey)}</span>
            </div>
            <strong className="v3-hero-stat__value">
              {stat.amount === null
                ? '—'
                : formatSensitiveMoney(stat.amount, locale, isAmountHidden)}
            </strong>
            <span className="v3-hero-stat__caption">{t(stat.captionKey)}</span>
          </article>
        ))}
      </div>
    </section>
  );
}
