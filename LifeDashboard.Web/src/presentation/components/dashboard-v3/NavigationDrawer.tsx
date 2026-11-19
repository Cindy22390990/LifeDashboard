import { useTranslation } from 'react-i18next';
import {
  changeAppLanguage,
  isSupportedLanguage,
  supportedLanguages,
} from '../../../i18n';
import { usePrivacyDisplayStore } from '../../stores/privacyDisplayStore';
import './NavigationDrawer.css';

interface NavigationDrawerProps {
  isOpen: boolean;
  onClose: () => void;
  onReportsClick: () => void;
}

const primaryNavigation = [
  { key: 'dashboard', icon: '⌂', active: true },
  { key: 'ledger', icon: '≡', active: false },
  { key: 'accounts', icon: '▣', active: false },
  { key: 'goals', icon: '◎', active: false },
  { key: 'reports', icon: '▤', active: false },
];

const managementNavigation = [
  { key: 'budgets', icon: '◷' },
  { key: 'fixedCommitments', icon: '▣' },
  { key: 'categories', icon: '◇' },
];

export default function NavigationDrawer({
  isOpen,
  onClose,
  onReportsClick,
}: NavigationDrawerProps) {
  const { t, i18n } = useTranslation();
  const activeLanguage = i18n.resolvedLanguage ?? i18n.language;
  const isAmountHidden = usePrivacyDisplayStore((state) => state.isAmountHidden);
  const toggleVisibility = usePrivacyDisplayStore((state) => state.toggleVisibility);

  return (
    <>
      <div
        aria-hidden={!isOpen}
        className={`v3-drawer-overlay${isOpen ? ' is-open' : ''}`}
        onClick={onClose}
      />
      <aside
        aria-label={t('navigation.ariaLabel')}
        aria-modal={isOpen}
        aria-hidden={!isOpen}
        className={`v3-navigation-drawer${isOpen ? ' is-open' : ''}`}
        inert={!isOpen}
        role="dialog"
      >
        <div className="v3-navigation-drawer__heading">
          <div>
            <span className="v3-navigation-drawer__brand-mark" aria-hidden="true">L</span>
            <strong>LifeDashboard</strong>
          </div>
          <button
            type="button"
            onClick={onClose}
            aria-label={t('navigation.close')}
          >
            ×
          </button>
        </div>
        <nav className="v3-navigation-drawer__menu">
          <p className="v3-navigation-drawer__section-label">
            {t('navigation.sections.overview')}
          </p>
          {primaryNavigation.map((item) => (
            <button
              aria-current={item.active ? 'page' : undefined}
              className={`v3-navigation-drawer__item${item.active ? ' is-active' : ''}`}
              key={item.key}
              onClick={item.key === 'reports' ? onReportsClick : undefined}
              type="button"
            >
              <span aria-hidden="true">{item.icon}</span>
              {t(`navigation.items.${item.key}`)}
            </button>
          ))}
          <p className="v3-navigation-drawer__section-label">
            {t('navigation.sections.management')}
          </p>
          {managementNavigation.map((item) => (
            <button className="v3-navigation-drawer__item" key={item.key} type="button">
              <span aria-hidden="true">{item.icon}</span>
              {t(`navigation.items.${item.key}`)}
            </button>
          ))}
          <p className="v3-navigation-drawer__section-label">
            {t('navigation.sections.system')}
          </p>
          <button className="v3-navigation-drawer__item" type="button">
            <span aria-hidden="true">⚙</span>
            {t('navigation.items.settings')}
          </button>
          <button className="v3-navigation-drawer__item" type="button">
            <span aria-hidden="true">◐</span>
            {t('navigation.items.darkMode')}
            <span className="v3-navigation-drawer__toggle" aria-hidden="true" />
          </button>
          <button
            aria-label={t(isAmountHidden ? 'privacy.showAmounts' : 'privacy.hideAmounts')}
            aria-pressed={isAmountHidden}
            className="v3-navigation-drawer__item"
            onClick={toggleVisibility}
            type="button"
          >
            <span aria-hidden="true">
              <svg fill="none" viewBox="0 0 24 24">
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
            </span>
            {t('privacy.display')}
            <span
              className={`v3-navigation-drawer__toggle${isAmountHidden ? ' is-active' : ''}`}
              aria-hidden="true"
            />
          </button>
          <button className="v3-navigation-drawer__item" type="button">
            <span aria-hidden="true">⇩</span>
            {t('navigation.items.dataExport')}
          </button>
          <button className="v3-navigation-drawer__item" type="button">
            <span aria-hidden="true">◇</span>
            {t('navigation.items.security')}
          </button>
          <label className="v3-navigation-drawer__language">
            <span>{t('navigation.language')}</span>
            <select
              onChange={async (event) => {
                const language = event.currentTarget.value;
                if (!isSupportedLanguage(language)) {
                  throw new RangeError(`Unsupported language: ${language}`);
                }
                await changeAppLanguage(language);
              }}
              value={activeLanguage}
            >
              {supportedLanguages.map((language) => (
                <option key={language.code} value={language.code}>
                  {language.label}
                </option>
              ))}
            </select>
          </label>
          <button className="v3-navigation-drawer__item v3-navigation-drawer__item--logout" type="button">
            <span aria-hidden="true">↪</span>
            {t('navigation.items.logout')}
          </button>
        </nav>
        <div className="v3-navigation-drawer__profile">
          <span className="v3-navigation-drawer__avatar" aria-hidden="true">C</span>
          <span>
            <strong>Cindy</strong>
            <small>{t('navigation.personalAccount')}</small>
          </span>
        </div>
      </aside>
    </>
  );
}
