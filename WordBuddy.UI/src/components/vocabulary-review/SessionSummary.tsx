import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

interface SessionSummaryProps {
  answered: number
  correct: number
  stoppedByTimeCap: boolean
}

/** End-of-session card: answered and correct counts. No self-rating. */
export function SessionSummary({ answered, correct, stoppedByTimeCap }: SessionSummaryProps): ReactElement {
  const { t } = useTranslation('review')
  return (
    <motion.div
      data-testid="session-summary"
      initial={{ opacity: 0, scale: 0.95 }}
      animate={{ opacity: 1, scale: 1 }}
      transition={{ duration: 0.3 }}
      className="flex flex-col items-center gap-3 rounded-wb-card bg-wb-surface-card p-10 text-center shadow-wb-card"
    >
      <span className="text-5xl">🎉</span>
      <p className="text-2xl font-bold text-wb-ink">{stoppedByTimeCap ? t('summary.timeUp') : t('summary.done')}</p>
      <p className="text-wb-ink-muted">
        {t('summary.result', { answered, correct })}
      </p>
      {stoppedByTimeCap && <p className="text-wb-ink-muted">{t('summary.rest')}</p>}
      <Link
        to="/progress"
        className="mt-4 rounded-wb-md bg-wb-primary-soft px-6 py-3 text-lg font-bold text-wb-ink hover:bg-wb-primary-soft-hover"
      >
        {t('summary.viewProgress')}
      </Link>
    </motion.div>
  )
}
