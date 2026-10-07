import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { Link } from 'react-router-dom'

interface SessionSummaryProps {
  answered: number
  correct: number
  stoppedByTimeCap: boolean
}

/** End-of-session card: answered and correct counts. No self-rating. */
export function SessionSummary({ answered, correct, stoppedByTimeCap }: SessionSummaryProps): ReactElement {
  return (
    <motion.div
      data-testid="session-summary"
      initial={{ opacity: 0, scale: 0.95 }}
      animate={{ opacity: 1, scale: 1 }}
      transition={{ duration: 0.3 }}
      className="flex flex-col items-center gap-3 rounded-wb-card bg-wb-surface-card p-10 text-center shadow-wb-card"
    >
      <span className="text-5xl">🎉</span>
      <p className="text-2xl font-bold text-wb-ink">{stoppedByTimeCap ? "Time's up — great work today!" : 'Review complete!'}</p>
      <p className="text-wb-ink-muted">
        You answered {answered} and got {correct} right.
      </p>
      {stoppedByTimeCap && <p className="text-wb-ink-muted">The rest will wait for next time.</p>}
      <Link
        to="/progress"
        className="mt-4 rounded-wb-md bg-wb-primary-soft px-6 py-3 text-lg font-bold text-wb-ink hover:bg-wb-primary-soft-hover"
      >
        View Progress
      </Link>
    </motion.div>
  )
}
