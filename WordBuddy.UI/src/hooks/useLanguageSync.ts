import { useEffect } from 'react'
import { isSupportedLanguage } from '../i18n/config'
import { applyLanguage } from '../i18n'
import { useAuthStore } from '../store/authStore'

/**
 * Applies the profile language once per login (when the user id changes).
 * A missing or unsupported profile language keeps the device choice. Logout keeps the current language.
 */
export function useLanguageSync(): void {
  const userId = useAuthStore((state) => state.user?.id)
  const preferredLanguage = useAuthStore((state) => state.user?.preferredLanguage)

  useEffect(() => {
    if (userId && isSupportedLanguage(preferredLanguage)) {
      void applyLanguage(preferredLanguage)
    }
    // Only when the user changes: later edits of the language go through the switcher.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [userId])
}
