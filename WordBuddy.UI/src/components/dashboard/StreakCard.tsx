import type { ReactElement } from 'react'
import type { WeekActivity } from '../../types'
import { CountUpStat } from '../CountUpStat'
import { cardClass } from '../support/supportUi'

type StreakCardProps = Readonly<{ streakDays: number; weeks: WeekActivity[] }>

/** Current streak and active days per week. */
export function StreakCard({ streakDays, weeks }: StreakCardProps): ReactElement {
  return (
    <section className={cardClass} aria-label="Streak">
      <h2 className="text-lg font-bold text-wb-ink">Streak</h2>
      <p className="mt-2 text-5xl font-extrabold text-wb-highlight">
        🔥 <CountUpStat value={streakDays} />
      </p>
      <p className="mt-1 text-sm text-wb-ink-muted">{streakDays === 1 ? 'day in a row' : 'days in a row'}</p>

      {weeks.length > 0 && (
        <ul className="mt-4 flex flex-col gap-1 text-sm text-wb-ink">
          {weeks.map((week) => (
            <li key={week.weekStart} className="flex justify-between gap-3">
              <span className="text-wb-ink-muted">Week of {week.weekStart}</span>
              <span className="font-semibold">{week.activeDays} / 7 days</span>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
