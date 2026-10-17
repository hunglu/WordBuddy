import { useEffect, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { isSupportedLanguage } from '../i18n/config'
import { applyLanguage } from '../i18n'
import { useAuthStore } from '../store/authStore'
import { useUpdatePreferredLanguage } from './useProfile'

/**
 * Applies the profile language on a real login only. A session restored after a page reload keeps the
 * device choice, so a language whose save failed is not reverted. On a real login, when the profile has
 * no language, the current device language is saved once. An unsupported profile code is left as it is. Logout keeps the current language.
 */
export function useLanguageSync(): void {
  const { i18n } = useTranslation()
  const userId = useAuthStore((state) => state.user?.id)
  const save = useUpdatePreferredLanguage()
  // The user id already handled: the one restored from storage at app start, then each real login.
  const handledUserId = useRef<string | undefined>(userId)

  useEffect(() => {
    if (!userId) {
      handledUserId.current = undefined
      return
    }
    const preferredLanguage: string | null | undefined = useAuthStore.getState().user?.preferredLanguage
    // A restored session keeps the device language and never writes to the profile.
    // A repeated run for the same user (StrictMode in dev) is skipped too.
    if (handledUserId.current === userId) return
    handledUserId.current = userId
    if (isSupportedLanguage(preferredLanguage)) {
      void applyLanguage(preferredLanguage)
    } else if (preferredLanguage == null && isSupportedLanguage(i18n.language)) {
      // Only a profile with no language is filled; an unsupported code is left as it is.
      save.mutate(i18n.language)
    }
    // Only when the user changes: later edits of the language go through the switcher.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [userId])
}
