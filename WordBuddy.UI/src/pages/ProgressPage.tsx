import { useQuery } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { Link } from 'react-router-dom'
import { getUserProgress } from '../api/progress'
import { CountUpStat } from '../components/CountUpStat'

export function ProgressPage(): ReactElement {
  const { data, isLoading, isError } = useQuery({
    queryKey: ['progress'],
    queryFn: getUserProgress,
  })

  if (isLoading) {
    return <p className="text-lg text-sky-600">Loading your progress…</p>
  }

  if (isError) {
    return <p className="text-lg text-rose-600">Couldn't load your progress right now.</p>
  }

  const completed = data?.filter((entry) => entry.isCompleted) ?? []
  const scores = completed
    .map((entry) => entry.scorePercent)
    .filter((score): score is number => score !== null)
  const averageScore = scores.length > 0 ? Math.round(scores.reduce((a, b) => a + b, 0) / scores.length) : 0

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <h1 className="text-3xl font-extrabold text-sky-900">My Progress</h1>

      <div className="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-3">
        <div className="rounded-3xl bg-white p-6 text-center shadow-sm">
          <p className="text-4xl font-extrabold text-sky-600">
            <CountUpStat value={completed.length} />
          </p>
          <p className="mt-1 text-sky-800">Lessons completed</p>
        </div>
        <div className="rounded-3xl bg-white p-6 text-center shadow-sm">
          <p className="text-4xl font-extrabold text-emerald-600">
            <CountUpStat value={averageScore} suffix="%" />
          </p>
          <p className="mt-1 text-sky-800">Average score</p>
        </div>
        <div className="rounded-3xl bg-white p-6 text-center shadow-sm">
          <p className="text-4xl font-extrabold text-amber-500">🔥</p>
          <p className="mt-1 text-sky-800">Streak (coming soon)</p>
        </div>
      </div>

      {completed.length === 0 ? (
        <div className="mt-10 flex flex-col items-center gap-3 rounded-3xl bg-white p-10 text-center shadow-sm">
          <span className="text-5xl">🌱</span>
          <p className="text-lg font-semibold text-sky-900">No completed lessons yet</p>
          <p className="text-sky-600">Finish your first lesson to see it here!</p>
          <Link
            to="/lessons"
            className="mt-2 rounded-xl bg-sky-500 px-6 py-3 text-lg font-bold text-white shadow hover:bg-sky-600"
          >
            Browse Lessons
          </Link>
        </div>
      ) : (
        <div className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {completed.map((entry) => (
            <div key={entry.id} className="rounded-3xl bg-white p-5 shadow-sm">
              <p className="text-lg font-bold text-sky-900">{entry.lessonTitle}</p>
              {entry.scorePercent !== null && (
                <span className="mt-2 inline-block rounded-full bg-emerald-100 px-3 py-1 text-sm font-semibold text-emerald-800">
                  {entry.scorePercent}%
                </span>
              )}
            </div>
          ))}
        </div>
      )}
    </motion.div>
  )
}
