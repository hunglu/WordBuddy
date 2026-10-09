import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import type { DailyGoal } from '../../types'
import { cardClass } from '../support/supportUi'

type DailyGoalBarProps = Readonly<{ goals: DailyGoal[] }>

const SHOWN_DAYS = 7

/** Share of the day's planned words answered, for the latest days that had a session. */
export function DailyGoalBar({ goals }: DailyGoalBarProps): ReactElement {
  const recent = goals.slice(-SHOWN_DAYS).reverse()

  return (
    <section className={cardClass} aria-label="Daily goal">
      <h2 className="text-lg font-bold text-wb-ink">Daily goal</h2>
      {recent.length === 0 ? (
        <p className="mt-2 text-wb-ink-muted">No sessions in this period yet.</p>
      ) : (
        <ul className="mt-3 flex flex-col gap-3">
          {recent.map((goal) => (
            <li key={goal.date}>
              <div className="flex justify-between text-sm text-wb-ink">
                <span>{goal.date}</span>
                <span className="font-semibold">
                  {goal.answeredWords} / {goal.plannedItems} words ({goal.percent}%)
                </span>
              </div>
              <div className="mt-1 h-3 overflow-hidden rounded-wb-pill bg-wb-hover-tint">
                <motion.div
                  className="h-full rounded-wb-pill bg-wb-success"
                  initial={{ width: 0 }}
                  animate={{ width: `${goal.percent}%` }}
                  transition={{ duration: 0.4 }}
                />
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
