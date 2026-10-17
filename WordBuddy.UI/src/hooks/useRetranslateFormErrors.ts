import { useEffect, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import type { FieldErrors, FieldValues, Path, UseFormTrigger } from 'react-hook-form'

/**
 * Re-validates the fields that already show an error when the UI language changes,
 * so the messages on screen switch language at once. Fields without an error stay untouched.
 */
export function useRetranslateFormErrors<T extends FieldValues>(
  errors: FieldErrors<T>,
  trigger: UseFormTrigger<T>,
): void {
  const { i18n } = useTranslation()
  const errorsRef = useRef<FieldErrors<T>>(errors)
  errorsRef.current = errors
  const previousLanguage = useRef<string>(i18n.language)

  useEffect(() => {
    if (previousLanguage.current === i18n.language) {
      return
    }
    previousLanguage.current = i18n.language
    const fields = Object.keys(errorsRef.current) as Path<T>[]
    if (fields.length > 0) {
      void trigger(fields)
    }
  }, [i18n.language, trigger])
}
