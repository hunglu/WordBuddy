import { useQuery } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { getUserProgress } from '../api/progress'
import { currentLocale } from '../i18n'
import { CountUpStat } from '../components/CountUpStat'
import { primaryButtonClass } from '../components/support/supportUi'
import { useVocabularyRecallProgress } from '../hooks/useVocabulary'

export function ProgressPage(): ReactElement {
  const { t } = useTranslation('dashboard')
  const { data, isLoading, isError } = useQuery({
    queryKey: ['progress'],
    queryFn: getUserProgress,
  })

  const {
    data: recall,
    isLoading: isRecallLoading,
    isError: isRecallError,
  } = useVocabularyRecallProgress()

  if (isLoading) {
    return <p className="text-lg text-wb-ink-muted">{t('progress.loading')}</p>
  }

  if (isError) {
    return <p className="text-lg text-wb-danger">{t('progress.loadFailed')}</p>
  }

  const completed = data?.filter((entry) => entry.isCompleted) ?? []
  const scores = completed
    .map((entry) => entry.scorePercent)
    .filter((score): score is number => score !== null)
  const averageScore = scores.length > 0 ? Math.round(scores.reduce((a, b) => a + b, 0) / scores.length) : 0

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-3xl font-extrabold text-wb-ink">{t('progress.title')}</h1>
        <Link
          to="/dashboard/me"
          className={primaryButtonClass}
        >
          {t('progress.myDashboard')}
        </Link>
      </div>

      <div className="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-3">
        <div className="rounded-wb-card bg-wb-surface-card p-6 text-center shadow-wb-card">
          <p className="text-4xl font-extrabold text-wb-ink-muted">
            <CountUpStat value={completed.length} />
          </p>
          <p className="mt-1 text-wb-ink">{t('progress.lessonsCompleted')}</p>
        </div>
        <div className="rounded-wb-card bg-wb-surface-card p-6 text-center shadow-wb-card">
          <p className="text-4xl font-extrabold text-wb-success">
            <CountUpStat value={averageScore} suffix="%" />
          </p>
          <p className="mt-1 text-wb-ink">{t('progress.averageScore')}</p>
        </div>
        <div className="rounded-wb-card bg-wb-surface-card p-6 text-center shadow-wb-card">
          <p className="text-4xl font-extrabold text-wb-highlight">🔥</p>
          <p className="mt-1 text-wb-ink">{t('progress.streakSoon')}</p>
        </div>
      </div>

      {completed.length === 0 ? (
        <div className="mt-10 flex flex-col items-center gap-3 rounded-wb-card bg-wb-surface-card p-10 text-center shadow-wb-card">
          <span className="text-5xl">🌱</span>
          <p className="text-lg font-semibold text-wb-ink">{t('progress.noneYet')}</p>
          <p className="text-wb-ink-muted">{t('progress.noneHint')}</p>
          <Link
            to="/lessons"
            className="mt-2 rounded-wb-md bg-wb-primary px-6 py-3 text-lg font-bold text-wb-on-primary shadow-wb-card hover:bg-wb-primary-hover"
          >
            {t('progress.browse')}
          </Link>
        </div>
      ) : (
        <div className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {completed.map((entry) => (
            <div key={entry.id} className="rounded-wb-card bg-wb-surface-card p-5 shadow-wb-card">
              <p className="text-lg font-bold text-wb-ink">{entry.lessonTitle}</p>
              {entry.scorePercent !== null && (
                <span className="mt-2 inline-block rounded-wb-pill bg-wb-success-soft px-3 py-1 text-sm font-semibold text-wb-success-soft-ink">
                  {entry.scorePercent}%
                </span>
              )}
            </div>
          ))}
        </div>
      )}

      <h2 className="mt-10 text-2xl font-extrabold text-wb-ink">{t('progress.recallHeading')}</h2>

      {isRecallLoading && <p className="mt-4 text-lg text-wb-ink-muted">{t('progress.recallLoading')}</p>}
      {isRecallError && (
        <p className="mt-4 text-lg text-wb-danger">{t('progress.recallFailed')}</p>
      )}

      {recall && (
        <>
          <div className="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-3">
            <div className="rounded-wb-card bg-wb-surface-card p-6 text-center shadow-wb-card">
              <p className="text-4xl font-extrabold text-wb-ink-muted">
                <CountUpStat value={recall.totalWordsTracked} />
              </p>
              <p className="mt-1 text-wb-ink">{t('progress.wordsTracked')}</p>
            </div>
            <div className="rounded-wb-card bg-wb-surface-card p-6 text-center shadow-wb-card">
              <p className="text-4xl font-extrabold text-wb-success">
                <CountUpStat value={recall.knownCount} />
              </p>
              <p className="mt-1 text-wb-ink">{t('progress.known')}</p>
            </div>
            <div className="rounded-wb-card bg-wb-surface-card p-6 text-center shadow-wb-card">
              <p className="text-4xl font-extrabold text-wb-highlight">
                <CountUpStat value={recall.learningCount} />
              </p>
              <p className="mt-1 text-wb-ink">{t('progress.stillLearning')}</p>
            </div>
          </div>

          {recall.recentSessions.length === 0 ? (
            <div className="mt-6 flex flex-col items-center gap-3 rounded-wb-card bg-wb-surface-card p-10 text-center shadow-wb-card">
              <span className="text-5xl">🧠</span>
              <p className="text-lg font-semibold text-wb-ink">{t('progress.noRecall')}</p>
              <p className="text-wb-ink-muted">{t('progress.noRecallHint')}</p>
              <Link
                to="/vocabulary/review"
                className="mt-2 rounded-wb-md bg-wb-success px-6 py-3 text-lg font-bold text-wb-on-success hover:bg-wb-success-hover shadow-wb-card"
              >
                {t('progress.startReview')}
              </Link>
            </div>
          ) : (
            <div className="mt-6 flex flex-col gap-3">
              {recall.recentSessions.map((session) => (
                <div key={session.id} className="flex items-center justify-between rounded-wb-lg bg-wb-surface-card p-4 shadow-wb-card">
                  <p className="text-wb-ink">{new Date(session.checkedAtUtc).toLocaleString(currentLocale())}</p>
                  <span className="rounded-wb-pill bg-wb-success-soft px-3 py-1 text-sm font-semibold text-wb-success-soft-ink">
                    {t('progress.sessionKnown', { known: session.wordsKnown, checked: session.wordsChecked })}
                  </span>
                </div>
              ))}
            </div>
          )}
        </>
      )}
    </motion.div>
  )
}
