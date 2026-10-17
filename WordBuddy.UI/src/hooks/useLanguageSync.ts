import { useEffect, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { isSupportedLanguage } from '../i18n/config'
import { applyLanguage } from '../i18n'
import { useAuthStore } from '../store/authStore'
import { useUpdatePreferredLanguage } from './useProfile'

/**
 * Applies the profile language on a real login only. A session restored after a page reload keeps the
 * device choice, so a language whose save failed is not reverted. When the profile has no language
 * (or an unsupported one), the current device language is saved once. Logout keeps the current language.
 */
export function useLanguageSync(): void {
  const { i18n } = useTranslation()
  const userId = useAuthStore((state) => state.user?.id)
  const save = useUpdatePreferredLanguage()
  // The user id restored from storage at app start; a login later has a different (or re-appearing) id.
  const restoredUserId = useRef<string | undefined>(userId)

  useEffect(() => {
    if (!userId) {
      restoredUserId.current = undefined
      return
    }
    const preferredLanguage: string | null | undefined = useAuthStore.getState().user?.preferredLanguage
    if (isSupportedLanguage(preferredLanguage)) {
      if (restoredUserId.current !== userId) {
        void applyLanguage(preferredLanguage)
      }
    } else if (isSupportedLanguage(i18n.language)) {
      save.mutate(i18n.language)
    }
    // Only when the user changes: later edits of the language go through the switcher.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [userId])
}
