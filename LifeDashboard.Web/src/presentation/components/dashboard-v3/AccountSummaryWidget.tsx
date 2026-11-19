import { mockDashboardData } from './mockDashboardData';
import { formatSensitiveMoney } from '../../../i18n/formatters';
import { useTranslation } from 'react-i18next';
import { usePrivacyDisplayStore } from '../../stores/privacyDisplayStore';
import './AccountSummaryWidget.css';

export default function AccountSummaryWidget() {
  const { t, i18n } = useTranslation();
  const locale = i18n.resolvedLanguage ?? i18n.language;
  const isAmountHidden = usePrivacyDisplayStore((state) => state.isAmountHidden);

  return (
    <section className="v3-widget v3-account-summary" aria-labelledby="v3-account-title">
      <div className="v3-widget__heading">
        <div>
          <p className="v3-section-eyebrow">{t('accounts.eyebrow')}</p>
          <h2 id="v3-account-title">{t('accounts.title')}</h2>
        </div>
        <button className="v3-text-button" type="button">{t('common.viewAll')}</button>
      </div>
      <ul className="v3-account-summary__list">
        {mockDashboardData.accounts.map((account) => (
          <li className="v3-account-summary__item" key={account.id}>
            <span className="v3-account-summary__icon" aria-hidden="true">▣</span>
            <span className="v3-account-summary__identity">
              <strong>{account.name}</strong>
              <small>{t(`accounts.types.${account.type}`)}</small>
            </span>
            <strong className="v3-account-summary__balance">
              {formatSensitiveMoney(account.balance, locale, isAmountHidden)}
            </strong>
          </li>
        ))}
      </ul>
    </section>
  );
}
