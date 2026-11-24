import { formatCurrency } from '../../../i18n/formatters';
import type { AccountSummaryViewModel } from '../../../application/dashboard/models/DashboardViewModel';
import type { SectionState } from '../../../application/dashboard/models/SectionState';
import { useTranslation } from 'react-i18next';
import SectionStateMessage from './SectionStateMessage';
import './AccountSummaryWidget.css';

interface AccountSummaryWidgetProps {
  state: SectionState<AccountSummaryViewModel[]>;
}

export default function AccountSummaryWidget({ state }: AccountSummaryWidgetProps) {
  const { t, i18n } = useTranslation();
  const locale = i18n.resolvedLanguage ?? i18n.language;

  return (
    <section className="v3-widget v3-account-summary" aria-labelledby="v3-account-title">
      <div className="v3-widget__heading">
        <div>
          <p className="v3-section-eyebrow">{t('accounts.eyebrow')}</p>
          <h2 id="v3-account-title">{t('accounts.title')}</h2>
        </div>
        <button className="v3-text-button" type="button">{t('common.viewAll')}</button>
      </div>
      {state.status === 'ready' ? (
        <ul className="v3-account-summary__list">
          {state.data.map((account) => (
            <li className="v3-account-summary__item" key={account.accountId}>
              <span className="v3-account-summary__icon" aria-hidden="true">▣</span>
              <span className="v3-account-summary__identity">
                <strong>{account.name}</strong>
                <small>{t(`accounts.types.${account.accountType}`, {
                  defaultValue: account.accountType,
                })}</small>
              </span>
              <strong className="v3-account-summary__balance">
                {formatCurrency(account.balance, locale)}
              </strong>
            </li>
          ))}
        </ul>
      ) : (
        <SectionStateMessage emptyMessage={t('accounts.empty')} state={state} />
      )}
    </section>
  );
}
