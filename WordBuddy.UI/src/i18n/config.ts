import { en } from './locales/en'
import { vi } from './locales/vi'

/** Languages the UI offers. The label is written in the language itself. */
export const SUPPORTED_LANGUAGES = [
  { code: 'en', label: 'English' },
  { code: 'vi', label: 'Tiếng Việt' },
] as const

export type LanguageCode = (typeof SUPPORTED_LANGUAGES)[number]['code']

export const DEFAULT_LANGUAGE: LanguageCode = 'en'

/** localStorage key of the device language choice. */
export const LANGUAGE_STORAGE_KEY = 'wordbuddy-lang'

export const resources = { en, vi }

/** Whether `code` is a language the UI supports. */
export function isSupportedLanguage(code: string | null | undefined): code is LanguageCode {
  return SUPPORTED_LANGUAGES.some((language) => language.code === code)
}
