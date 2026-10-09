import type { ReactElement } from 'react'
import type { DashboardRetention } from '../../types'
import { cardClass } from '../support/supportUi'

type RetentionCardProps = Readonly<{ retention: DashboardRetention }>

/** Hero number: true retention (first attempt of due words), overall and per skill. */
export function RetentionCard({ retention }: RetentionCardProps): ReactElement {
  return (
    <section className={cardClass} aria-label="Retention">
      <h2 className="text-lg font-bold text-wb-ink">Retention</h2>
      {retention.overall === null ? (
        <>
          <p className="mt-2 text-3xl font-extrabold text-wb-ink-muted">Not enough answers yet</p>
          <p className="mt-1 text-sm text-wb-ink-muted">{retention.sample} counted so far. A few more reviews will show a number.</p>
        </>
      ) : (
        <>
          <p className="mt-2 text-6xl font-extrabold text-wb-success">{retention.overall}%</p>
          <p className="mt-1 text-sm text-wb-ink-muted">
            Right on the first try, in {retention.sample} reviews of words that were due.
          </p>
        </>
      )}

      {retention.bySkill.length > 0 && (
        <ul className="mt-4 flex flex-wrap gap-2">
          {retention.bySkill.map((skill) => (
            <li key={skill.skill} className="rounded-wb-pill bg-wb-hover-tint px-3 py-1 text-sm font-semibold text-wb-ink">
              {skill.skill}: {skill.retention === null ? 'not enough data' : `${skill.retention}%`}
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
