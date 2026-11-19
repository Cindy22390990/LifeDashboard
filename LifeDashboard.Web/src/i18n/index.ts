import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import en from './locales/en.json';
import ja from './locales/ja.json';
import ko from './locales/ko.json';
import ptBR from './locales/pt-BR.json';
import zhTW from './locales/zh-TW.json';

export const languageStorageKey = 'lifedashboard.language';

export const supportedLanguages = [
  { code: 'en', label: 'English' },
  { code: 'zh-TW', label: '繁體中文' },
  { code: 'pt-BR', label: 'Português' },
  { code: 'ko', label: '한국어' },
  { code: 'ja', label: '日本語' },
] as const;

export type SupportedLanguage = (typeof supportedLanguages)[number]['code'];

export function isSupportedLanguage(value: string): value is SupportedLanguage {
  return supportedLanguages.some((language) => language.code === value);
}

function normalizeLanguage(value: string | undefined): SupportedLanguage | undefined {
  if (!value) return undefined;
  const normalized = value.replace('_', '-').toLowerCase();

  if (
    normalized === 'zh'
    || normalized === 'zh-tw'
    || normalized === 'zh-hk'
    || normalized === 'zh-mo'
    || normalized === 'zh-hant'
    || normalized.startsWith('zh-hant-')
  ) return 'zh-TW';
  if (normalized === 'pt' || normalized.startsWith('pt-')) return 'pt-BR';
  if (normalized === 'ko' || normalized.startsWith('ko-')) return 'ko';
  if (normalized === 'ja' || normalized.startsWith('ja-')) return 'ja';
  if (normalized === 'en' || normalized.startsWith('en-')) return 'en';

  return undefined;
}

function getInitialLanguage(): SupportedLanguage {
  const savedLanguage = normalizeLanguage(
    window.localStorage.getItem(languageStorageKey) ?? undefined,
  );
  if (savedLanguage) return savedLanguage;

  const browserLanguages = navigator.languages.length > 0
    ? navigator.languages
    : [navigator.language];

  return browserLanguages
    .map(normalizeLanguage)
    .find((language): language is SupportedLanguage => language !== undefined)
    ?? 'en';
}

i18n.on('languageChanged', (language) => {
  document.documentElement.lang = normalizeLanguage(language) ?? 'en';
});

export const i18nReady = i18n
  .use(initReactI18next)
  .init({
    lng: getInitialLanguage(),
    fallbackLng: 'en',
    supportedLngs: supportedLanguages.map(({ code }) => code),
    resources: {
      en: { translation: en },
      'zh-TW': { translation: zhTW },
      'pt-BR': { translation: ptBR },
      ko: { translation: ko },
      ja: { translation: ja },
    },
    interpolation: {
      escapeValue: false,
    },
    react: {
      useSuspense: false,
    },
  });

export async function changeAppLanguage(language: SupportedLanguage): Promise<void> {
  await i18n.changeLanguage(language);
  window.localStorage.setItem(languageStorageKey, language);
}

export default i18n;
