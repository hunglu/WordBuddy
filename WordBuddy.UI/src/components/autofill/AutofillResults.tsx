import axios from 'axios'
import type { ReactElement } from 'react'
import { useAddAutofillSense, useAutofillLookup } from '../../hooks/useAutofill'
import { SenseCard } from './SenseCard'

type AutofillResultsProps = Readonly<{
  /** The submitted word; `null` until the learner clicks "Auto-fill". */
  word: string | null
}>

/**
 * Auto-fill result for the add-word form: sense cards with "Add". When auto-fill is unavailable
 * (or the call fails) it tells the learner to use the manual form below, which keeps the word.
 */
export function AutofillResults({ word }: AutofillResultsProps): ReactElement | null {
  const lookup = useAutofillLookup(word)
  const addSense = useAddAutofillSense()

  if (word === null) return null

  if (lookup.isLoading) {
    return <p className="text-wb-ink-muted">Looking up "{word}"…</p>
  }

  const rateLimited = axios.isAxiosError(lookup.error) && lookup.error.response?.status === 429
  if (lookup.isError || !lookup.data || lookup.data.autofillUnavailable || lookup.data.senses.length === 0) {
    return (
      <p role="status" className="rounded-wb-md bg-wb-hover-tint px-4 py-3 text-wb-ink">
        {rateLimited
          ? 'Too many look-ups. Wait a minute, or fill in the form below.'
          : 'Auto-fill is not available for this word. Fill in the definition below instead.'}
      </p>
    )
  }

  return (
    <div className="flex flex-col gap-3">
      <p className="text-sm text-wb-ink-muted">Pick the meaning you want to learn.</p>
      <div className="grid gap-3 sm:grid-cols-2">
        {lookup.data.senses.map((sense, index) => {
          const added = addSense.isSuccess && addSense.variables === sense.senseId
          return (
            <SenseCard key={sense.senseId} sense={sense} index={index}>
              <button
                type="button"
                disabled={addSense.isPending || added}
                onClick={() => addSense.mutate(sense.senseId)}
                className="rounded-wb-md bg-wb-success px-4 py-2 text-sm font-bold text-wb-on-success shadow-wb-card hover:bg-wb-success-hover disabled:opacity-60"
              >
                {added ? 'Added' : 'Add'}
              </button>
            </SenseCard>
          )
        })}
      </div>
      {addSense.isError && <p className="text-sm text-wb-danger">Couldn't add this word right now. Please try again.</p>}
    </div>
  )
}
