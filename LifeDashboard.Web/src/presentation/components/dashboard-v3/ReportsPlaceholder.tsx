import { useTranslation } from 'react-i18next';
import './ReportsPlaceholder.css';

const reportSections = [
  { key: 'spendingTrend' },
  { key: 'incomeExpense' },
  { key: 'categorySpending' },
  { key: 'netWorthTrend' },
];

export default function ReportsPlaceholder() {
  const { t } = useTranslation();

  return (
    <section
      className="v3-reports-placeholder"
      id="reports-placeholder"
      aria-labelledby="v3-reports-title"
    >
      <div className="v3-reports-placeholder__heading">
        <div>
          <p className="v3-section-eyebrow">{t('reports.eyebrow')}</p>
          <h2 id="v3-reports-title">{t('reports.title')}</h2>
        </div>
        <span>{t('reports.comingSoon')}</span>
      </div>
      <div className="v3-reports-placeholder__grid">
        {reportSections.map((section) => (
          <article className="v3-reports-placeholder__card" key={section.key}>
            <span className="v3-reports-placeholder__icon" aria-hidden="true">▤</span>
            <h3>{t(`reports.sections.${section.key}.title`)}</h3>
            <p>{t(`reports.sections.${section.key}.description`)}</p>
          </article>
        ))}
      </div>
      <p className="v3-reports-placeholder__note">
        {t('reports.phaseNote')}
      </p>
    </section>
  );
}
