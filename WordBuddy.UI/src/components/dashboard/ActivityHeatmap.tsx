import type { ReactElement } from 'react'
import type { DayActivity } from '../../types'
import { cardClass } from '../support/supportUi'
import styles from './ActivityHeatmap.module.css'

type ActivityHeatmapProps = Readonly<{ days: DayActivity[] }>

const LEVEL_CLASSES: readonly string[] = [styles.level0, styles.level1, styles.level2, styles.level3, styles.level4]

function levelOf(reviews: number, max: number): number {
  if (reviews === 0 || max === 0) {
    return 0
  }
  return Math.max(1, Math.ceil((reviews / max) * 4))
}

/** Reviews per local day, one cell per day (Monday-first columns are not needed: a day strip is enough). */
export function ActivityHeatmap({ days }: ActivityHeatmapProps): ReactElement {
  const max = days.reduce((highest, day) => Math.max(highest, day.reviews), 0)
  // Pad so the first day lands on its weekday row (grid rows run Monday to Sunday).
  const first = days.length > 0 ? new Date(`${days[0].date}T00:00:00`) : null
  const padding = first === null ? 0 : (first.getDay() + 6) % 7

  return (
    <section className={cardClass} aria-label="Activity">
      <h2 className="text-lg font-bold text-wb-ink">Activity</h2>
      <div className="mt-3 overflow-x-auto">
        <div className={styles.grid}>
          {Array.from({ length: padding }, (_, index) => (
            <span key={`pad-${index}`} className={styles.cell} aria-hidden="true" />
          ))}
          {days.map((day) => (
            <span
              key={day.date}
              className={`${styles.cell} ${LEVEL_CLASSES[levelOf(day.reviews, max)]}`}
              title={`${day.date}: ${day.reviews} reviews`}
            />
          ))}
        </div>
      </div>
      <p className="mt-2 text-sm text-wb-ink-muted">Darker means more reviews that day.</p>
    </section>
  )
}
