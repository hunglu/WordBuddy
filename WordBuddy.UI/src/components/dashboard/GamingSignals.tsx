import type { ReactElement } from 'react'
import type { DashboardGamingSignals } from '../../types'
import { cardClass } from '../support/supportUi'

type GamingSignalsProps = Readonly<{ gaming: DashboardGamingSignals }>

/** Answer-pattern facts. Neutral wording on purpose: these are hints to talk about, not a verdict. */
export function GamingSignals({ gaming }: GamingSignalsProps): ReactElement {
  return (
    <section className={cardClass} aria-label="Answer patterns">
      <h2 className="text-lg font-bold text-wb-ink">Answer patterns</h2>
      {gaming.totalAnswers === 0 ? (
        <p className="mt-2 text-wb-ink-muted">No answers in this period yet.</p>
      ) : (
        <dl className="mt-3 flex flex-col gap-2 text-sm text-wb-ink">
          <div className="flex justify-between gap-3">
            <dt>Quick wrong answers</dt>
            <dd className="font-semibold">
              {gaming.quickWrongCount} ({gaming.quickWrongPercent}%)
            </dd>
          </div>
          <div className="flex justify-between gap-3">
            <dt>Answers with a hint</dt>
            <dd className="font-semibold">
              {gaming.hintPercent}%{gaming.hintRateFlagged ? ' (higher than usual)' : ''}
            </dd>
          </div>
          <div className="flex justify-between gap-3">
            <dt>Sessions not finished</dt>
            <dd className="font-semibold">{gaming.unfinishedSessions}</dd>
          </div>
        </dl>
      )}
    </section>
  )
}
