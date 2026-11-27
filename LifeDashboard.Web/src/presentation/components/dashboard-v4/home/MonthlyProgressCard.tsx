import { useTranslation } from 'react-i18next';
import { formatCurrency } from '../../../../i18n/formatters';
import type { V4HomeViewModel } from '../../../../mappers/dashboardV4';

interface MonthlyProgressCardProps {
  viewModel: V4HomeViewModel['monthlyProgress'];
}

export function MonthlyProgressCard({ viewModel }: MonthlyProgressCardProps) {
  const { t, i18n } = useTranslation();
  const locale = i18n.resolvedLanguage ?? i18n.language;

  return (
    <section
      aria-labelledby="v4-monthly-progress-title"
      className="v4-home-card v4-monthly-progress"
    >
      <h2 id="v4-monthly-progress-title">
        {t('dashboard.monthlyProgress.title')}
      </h2>
      <dl className="v4-monthly-progress__values">
        <div>
          <dt>{t('dashboard.monthlyProgress.saved')}</dt>
          <dd>{formatCurrency(viewModel.saved, locale)}</dd>
        </div>
        <div>
          <dt>{t('dashboard.monthlyProgress.invested')}</dt>
          <dd>{formatCurrency(viewModel.invested, locale)}</dd>
        </div>
      </dl>
    </section>
  );
}
