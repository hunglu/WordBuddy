import type { ReactElement } from 'react'
import { shareStatusClasses } from '../../theme/variants'
import type { AdminLexemeSense } from '../../types'
import { cardClass } from '../support/supportUi'

const SOURCE_LABELS: Record<AdminLexemeSense['source'], string> = {
  System: 'Lesson word',
  Learner: 'Community word',
}

const SHARE_LABELS: Record<AdminLexemeSense['shareStatus'], string> = {
  Private: 'Private',
  PendingReview: 'Waiting for review',
  Shared: 'Shared',
  Rejected: 'Rejected',
}

type LexemeSensesPanelProps = Readonly<{
  /** Catalog senses only (lesson words and shared-pool words). */
  senses: readonly AdminLexemeSense[]
  /** Private, pending and rejected learner senses: only the count is known. */
  privateLearnerSenseCount: number
}>

/** Read-only list of the meanings of a lexeme. Private learner text is never shown. */
export function LexemeSensesPanel({ senses, privateLearnerSenseCount }: LexemeSensesPanelProps): ReactElement {
  return (
    <section className="mt-6">
      <h2 className="text-xl font-bold text-wb-ink">Senses ({senses.length})</h2>
      {senses.length === 0 && <p className="mt-2 text-wb-ink-muted">This lexeme has no catalog sense yet.</p>}

      <ul className="mt-3 flex flex-col gap-2">
        {senses.map((sense) => (
          <li key={sense.id} className={cardClass}>
            <div className="flex flex-wrap items-center gap-2">
              <span className="font-bold text-wb-ink">{sense.word}</span>
              <span className="rounded-wb-pill border border-wb-border-subtle px-2 py-0.5 text-xs font-semibold text-wb-ink">
                {SOURCE_LABELS[sense.source]}
              </span>
              <span className={`rounded-wb-pill border px-2 py-0.5 text-xs font-semibold ${shareStatusClasses[sense.shareStatus]}`}>
                {SHARE_LABELS[sense.shareStatus]}
              </span>
              <span className="rounded-wb-pill border border-wb-border-subtle px-2 py-0.5 text-xs font-semibold text-wb-ink">
                {sense.origin === 'AutoFill' ? 'Auto-filled' : 'Written by hand'}
              </span>
              <span className="rounded-wb-pill border border-wb-border-subtle px-2 py-0.5 text-xs font-semibold text-wb-ink">
                {sense.visibleToChildren ? 'Visible to children' : 'Hidden from children'}
              </span>
            </div>
            <p className="mt-2 text-wb-ink">{sense.definition}</p>
            {sense.example && <p className="mt-1 text-sm italic text-wb-ink-muted">{sense.example}</p>}
          </li>
        ))}
      </ul>

      <p className="mt-3 text-sm text-wb-ink-muted">
        {privateLearnerSenseCount} private learner {privateLearnerSenseCount === 1 ? 'sense' : 'senses'} (not shown)
      </p>
    </section>
  )
}
