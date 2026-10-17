import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import { DEFAULT_LANGUAGE, LANGUAGE_STORAGE_KEY, isSupportedLanguage, resources, type LanguageCode } from './config'

function readStoredLanguage(): string | null {
  try {
    return window.localStorage.getItem(LANGUAGE_STORAGE_KEY)
  } catch {
    return null
  }
}

/** Device choice (localStorage), else the first browser language that starts with a supported code, else `en`. */
export function resolveInitialLanguage(): LanguageCode {
  const stored: string | null = readStoredLanguage()
  if (isSupportedLanguage(stored)) {
    return stored
  }
  const browser: readonly string[] = typeof navigator === 'undefined' ? [] : (navigator.languages ?? [navigator.language])
  for (const tag of browser) {
    const base: string = tag.toLowerCase().split('-')[0]
    if (isSupportedLanguage(base)) {
      return base
    }
  }
  return DEFAULT_LANGUAGE
}

/** Initializes i18next once. Safe to call again. */
export function initI18n(): typeof i18n {
  if (!i18n.isInitialized) {
    const language: LanguageCode = resolveInitialLanguage()
    void i18n.use(initReactI18next).init({
      resources,
      lng: language,
      fallbackLng: DEFAULT_LANGUAGE,
      defaultNS: 'common',
      ns: Object.keys(resources.en),
      interpolation: { escapeValue: false },
      returnNull: false,
      initAsync: false,
    })
    document.documentElement.lang = language
  }
  return i18n
}

/** Switches the UI language at once (no reload), remembers it on this device and sets `<html lang>`. */
export async function applyLanguage(code: LanguageCode): Promise<void> {
  await i18n.changeLanguage(code)
  try {
    window.localStorage.setItem(LANGUAGE_STORAGE_KEY, code)
  } catch {
    // Storage can be blocked; the language still applies for this session.
  }
  document.documentElement.lang = code
}

/** The current UI language, used as the locale of dates and numbers. */
export function currentLocale(): string {
  return i18n.language || DEFAULT_LANGUAGE
}

initI18n()

export default i18n
