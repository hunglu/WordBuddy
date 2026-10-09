import type { ReactElement } from 'react'
import { useSenses } from '../../hooks/useVocabularyReview'
import type { DashboardStruggle } from '../../types'
import { cardClass } from '../support/supportUi'

type StruggleListProps = Readonly<{ struggle: DashboardStruggle }>

const UNKNOWN_WORD = 'Word not available'

/** Words that need attention. Progress returns sense ids only; text comes from Content. */
export function StruggleList({ struggle }: StruggleListProps): ReactElement {
  const ids = [...new Set([...struggle.leeches.map((word) => word.senseId), ...struggle.slowestWords.map((word) => word.senseId)])]
  // If Content fails or hides a word, the list still renders with a placeholder.
  const senses = useSenses(ids)
  const wordOf = (senseId: string): string =>
    senses.data?.find((sense) => sense.senseId === senseId)?.word ?? UNKNOWN_WORD

  return (
    <section className={cardClass} aria-label="Needs attention">
      <h2 className="text-lg font-bold text-wb-ink">Needs attention</h2>

      <p className="mt-2 text-sm text-wb-ink">
        Weakest skill:{' '}
        <span className="font-semibold">{struggle.weakestSkill ?? 'not enough data yet'}</span>
      </p>

      <h3 className="mt-4 font-semibold text-wb-ink">Words that keep slipping</h3>
      {struggle.leeches.length === 0 ? (
        <p className="text-sm text-wb-ink-muted">None right now.</p>
      ) : (
        <ul className="mt-1 flex flex-col gap-1 text-sm text-wb-ink">
          {struggle.leeches.map((word) => (
            <li key={word.senseId} className="flex justify-between gap-3">
              <span>{wordOf(word.senseId)}</span>
              <span className="text-wb-ink-muted">forgotten {word.lapses}×</span>
            </li>
          ))}
        </ul>
      )}

      <h3 className="mt-4 font-semibold text-wb-ink">Words that take longest</h3>
      {struggle.slowestWords.length === 0 ? (
        <p className="text-sm text-wb-ink-muted">Not enough answers yet.</p>
      ) : (
        <ul className="mt-1 flex flex-col gap-1 text-sm text-wb-ink">
          {struggle.slowestWords.map((word) => (
            <li key={word.senseId} className="flex justify-between gap-3">
              <span>{wordOf(word.senseId)}</span>
              <span className="text-wb-ink-muted">about {(word.medianResponseMs / 1000).toFixed(1)} s</span>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
