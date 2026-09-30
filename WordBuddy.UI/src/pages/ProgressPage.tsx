import { useQuery } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { Link } from 'react-router-dom'
import { getUserProgress } from '../api/progress'
import { CountUpStat } from '../components/CountUpStat'
import { useVocabularyRecallProgress } from '../hooks/useVocabulary'

export function ProgressPage(): ReactElement {
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
    return <p className="text-lg text-wb-ink-muted">Loading your progress…</p>
  }

  if (isError) {
    return <p className="text-lg text-wb-danger">Couldn't load your progress right now.</p>
  }

  const completed = data?.filter((entry) => entry.isCompleted) ?? []
  const scores = completed
    .map((entry) => entry.scorePercent)
    .filter((score): score is number => score !== null)
  const averageScore = scores.length > 0 ? Math.round(scores.reduce((a, b) => a + b, 0) / scores.length) : 0

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <h1 className="text-3xl font-extrabold text-wb-ink">My Progress</h1>

      <div className="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-3">
        <div className="rounded-wb-card bg-wb-surface-card p-6 text-center shadow-wb-card">
          <p className="text-4xl font-extrabold text-wb-ink-muted">
            <CountUpStat value={completed.length} />
          </p>
          <p className="mt-1 text-wb-ink">Lessons completed</p>
        </div>
        <div className="rounded-wb-card bg-wb-surface-card p-6 text-center shadow-wb-card">
          <p className="text-4xl font-extrabold text-wb-success">
            <CountUpStat value={averageScore} suffix="%" />
          </p>
          <p className="mt-1 text-wb-ink">Average score</p>
        </div>
        <div className="rounded-wb-card bg-wb-surface-card p-6 text-center shadow-wb-card">
          <p className="text-4xl font-extrabold text-wb-highlight">🔥</p>
          <p className="mt-1 text-wb-ink">Streak (coming soon)</p>
        </div>
      </div>

      {completed.length === 0 ? (
        <div className="mt-10 flex flex-col items-center gap-3 rounded-wb-card bg-wb-surface-card p-10 text-center shadow-wb-card">
          <span className="text-5xl">🌱</span>
          <p className="text-lg font-semibold text-wb-ink">No completed lessons yet</p>
          <p className="text-wb-ink-muted">Finish your first lesson to see it here!</p>
          <Link
            to="/lessons"
            className="mt-2 rounded-wb-md bg-wb-primary px-6 py-3 text-lg font-bold text-wb-on-primary shadow-wb-card hover:bg-wb-primary-hover"
          >
            Browse Lessons
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

      <h2 className="mt-10 text-2xl font-extrabold text-wb-ink">Vocabulary Recall</h2>

      {isRecallLoading && <p className="mt-4 text-lg text-wb-ink-muted">Loading your recall progress…</p>}
      {isRecallError && (
        <p className="mt-4 text-lg text-wb-danger">Couldn't load your recall progress right now.</p>
      )}

      {recall && (
        <>
          <div className="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-3">
            <div className="rounded-wb-card bg-wb-surface-card p-6 text-center shadow-wb-card">
              <p className="text-4xl font-extrabold text-wb-ink-muted">
                <CountUpStat value={recall.totalWordsTracked} />
              </p>
              <p className="mt-1 text-wb-ink">Words tracked</p>
            </div>
            <div className="rounded-wb-card bg-wb-surface-card p-6 text-center shadow-wb-card">
              <p className="text-4xl font-extrabold text-wb-success">
                <CountUpStat value={recall.knownCount} />
              </p>
              <p className="mt-1 text-wb-ink">Known</p>
            </div>
            <div className="rounded-wb-card bg-wb-surface-card p-6 text-center shadow-wb-card">
              <p className="text-4xl font-extrabold text-wb-highlight">
                <CountUpStat value={recall.learningCount} />
              </p>
              <p className="mt-1 text-wb-ink">Still learning</p>
            </div>
          </div>

          {recall.recentSessions.length === 0 ? (
            <div className="mt-6 flex flex-col items-center gap-3 rounded-wb-card bg-wb-surface-card p-10 text-center shadow-wb-card">
              <span className="text-5xl">🧠</span>
              <p className="text-lg font-semibold text-wb-ink">No recall checks yet</p>
              <p className="text-wb-ink-muted">Run your first check to see your history here!</p>
              <Link
                to="/vocabulary/check"
                className="mt-2 rounded-wb-md bg-wb-success px-6 py-3 text-lg font-bold text-wb-on-success hover:bg-wb-success-hover shadow-wb-card"
              >
                Start a Check
              </Link>
            </div>
          ) : (
            <div className="mt-6 flex flex-col gap-3">
              {recall.recentSessions.map((session) => (
                <div key={session.id} className="flex items-center justify-between rounded-wb-lg bg-wb-surface-card p-4 shadow-wb-card">
                  <p className="text-wb-ink">{new Date(session.checkedAtUtc).toLocaleString()}</p>
                  <span className="rounded-wb-pill bg-wb-success-soft px-3 py-1 text-sm font-semibold text-wb-success-soft-ink">
                    {session.wordsKnown} / {session.wordsChecked} known
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
