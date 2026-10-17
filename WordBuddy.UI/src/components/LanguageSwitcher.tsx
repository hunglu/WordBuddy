import type { ChangeEvent, ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import { SUPPORTED_LANGUAGES, isSupportedLanguage } from '../i18n/config'
import { applyLanguage } from '../i18n'
import { useUpdatePreferredLanguage } from '../hooks/useProfile'
import { useAuthStore } from '../store/authStore'

/**
 * Switches the UI language at once (no reload) and remembers it on this device.
 * When logged in it also saves the choice to the profile; a save failure keeps the new language.
 */
export function LanguageSwitcher(): ReactElement {
  const { t, i18n } = useTranslation('common')
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated)
  const save = useUpdatePreferredLanguage()

  function onChange(event: ChangeEvent<HTMLSelectElement>): void {
    const code: string = event.target.value
    if (!isSupportedLanguage(code)) {
      return
    }
    void applyLanguage(code)
    if (isAuthenticated) {
      save.mutate(code)
    }
  }

  return (
    <div className="flex flex-col gap-1">
      <select
        value={isSupportedLanguage(i18n.language) ? i18n.language : 'en'}
        onChange={onChange}
        aria-label={t('language.label')}
        className="w-full rounded-wb-md border-2 border-wb-border-control bg-wb-surface-card px-3 py-2 text-sm font-semibold text-wb-ink focus:border-wb-primary"
      >
        {SUPPORTED_LANGUAGES.map((language) => (
          <option key={language.code} value={language.code}>
            {language.label}
          </option>
        ))}
      </select>
      {save.isError && <p className="text-xs text-wb-danger">{t('language.saveFailed')}</p>}
    </div>
  )
}
