import { useTranslation } from 'react-i18next';
import { formatCurrency, formatDate } from '../../../../i18n/formatters';
import type { V4HomeViewModel } from '../../../../mappers/dashboardV4';

interface DailyDecisionHeroProps {
  viewModel: V4HomeViewModel;
}

export function DailyDecisionHero({ viewModel }: DailyDecisionHeroProps) {
  const { t, i18n } = useTranslation();
  const locale = i18n.resolvedLanguage ?? i18n.language;

  return (
    <section
      aria-labelledby="v4-daily-decision-title"
      className="v4-home-card v4-home-hero"
    >
      <p className="v4-home__eyebrow">
        {formatDate(viewModel.businessDate.isoDate, locale)}
      </p>
      <h2 id="v4-daily-decision-title">{t('dashboard.hero.leftToSpend')}</h2>
      <strong
        className={`v4-home-hero__remaining${viewModel.dailyRemaining < 0
          ? ' v4-home-hero__remaining--negative'
          : ''}`}
      >
        {formatCurrency(viewModel.dailyRemaining, locale)}
      </strong>
      <dl className="v4-home-hero__details">
        <div>
          <dt>{t('dashboard.hero.spentToday')}</dt>
          <dd>{formatCurrency(viewModel.dailySpent, locale)}</dd>
        </div>
        <div>
          <dt>{t('dashboard.hero.dailyLimit')}</dt>
          <dd>{formatCurrency(viewModel.dailySpendingLimit, locale)}</dd>
        </div>
      </dl>
    </section>
  );
}
