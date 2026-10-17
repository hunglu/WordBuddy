import axios from 'axios'
import type { ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import { useAddAutofillSense, useAutofillLookup } from '../../hooks/useAutofill'
import { SenseCard } from './SenseCard'

type AutofillResultsProps = Readonly<{
  /** The submitted word; `null` until the learner clicks "Auto-fill". */
  word: string | null
  /** Private note from the add form; sent with "Add". */
  personalContext?: string
  /** Called after a sense was added, so the form can clear the note (it must not leak into the next add). */
  onAdded?: () => void
}>

/**
 * Auto-fill result for the add-word form: sense cards with "Add". When auto-fill is unavailable
 * (or the call fails) it tells the learner to use the manual form below, which keeps the word.
 */
export function AutofillResults({ word, personalContext, onAdded }: AutofillResultsProps): ReactElement | null {
  const { t } = useTranslation('vocabulary')
  const lookup = useAutofillLookup(word)
  const addSense = useAddAutofillSense()

  if (word === null) return null

  if (lookup.isLoading) {
    return <p className="text-wb-ink-muted">{t('autofill.lookingUp', { word })}</p>
  }

  const rateLimited = axios.isAxiosError(lookup.error) && lookup.error.response?.status === 429
  if (lookup.isError || !lookup.data || lookup.data.autofillUnavailable || lookup.data.senses.length === 0) {
    return (
      <p role="status" className="rounded-wb-md bg-wb-hover-tint px-4 py-3 text-wb-ink">
        {rateLimited
          ? t('autofill.rateLimited')
          : t('autofill.unavailable')}
      </p>
    )
  }

  return (
    <div className="flex flex-col gap-3">
      <p className="text-sm text-wb-ink-muted">{t('autofill.pick')}</p>
      <div className="grid gap-3 sm:grid-cols-2">
        {lookup.data.senses.map((sense, index) => {
          const added = addSense.isSuccess && addSense.variables?.senseId === sense.senseId
          return (
            <SenseCard key={sense.senseId} sense={sense} index={index}>
              <button
                type="button"
                disabled={addSense.isPending || added}
                onClick={() =>
                  addSense.mutate(
                    { senseId: sense.senseId, personalContext: personalContext?.trim() || undefined },
                    { onSuccess: () => onAdded?.() },
                  )
                }
                className="rounded-wb-md bg-wb-success px-4 py-2 text-sm font-bold text-wb-on-success shadow-wb-card hover:bg-wb-success-hover disabled:opacity-60"
              >
                {added ? t('autofill.added') : t('autofill.add')}
              </button>
            </SenseCard>
          )
        })}
      </div>
      {addSense.isError && <p className="text-sm text-wb-danger">{t('autofill.addFailed')}</p>}
    </div>
  )
}
