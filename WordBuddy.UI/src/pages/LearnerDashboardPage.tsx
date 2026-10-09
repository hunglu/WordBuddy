import axios from 'axios'
import { motion } from 'framer-motion'
import { useState } from 'react'
import type { ReactElement } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ActivityHeatmap } from '../components/dashboard/ActivityHeatmap'
import { DailyGoalBar } from '../components/dashboard/DailyGoalBar'
import { GamingSignals } from '../components/dashboard/GamingSignals'
import { RetentionCard } from '../components/dashboard/RetentionCard'
import { StreakCard } from '../components/dashboard/StreakCard'
import { StruggleList } from '../components/dashboard/StruggleList'
import { WordStatusBreakdown } from '../components/dashboard/WordStatusBreakdown'
import { WordsAddedChart } from '../components/dashboard/WordsAddedChart'
import { softButtonClass, primaryButtonClass } from '../components/support/supportUi'
import { useLearnerDashboard, useMyDashboard } from '../hooks/useDashboard'
import type { DashboardRange } from '../types'

const RANGES: readonly DashboardRange[] = [7, 30, 90]

/**
 * `/dashboard/me` (own view) and `/dashboard/learners/:learnerId` (supporter view). Same page, same
 * widgets: every active supporter gets the full view. Dates only, never a time of day.
 */
export function LearnerDashboardPage(): ReactElement {
  const { learnerId } = useParams<{ learnerId: string }>()
  const [days, setDays] = useState<DashboardRange>(30)

  const mine = useMyDashboard(days, learnerId === undefined)
  const learner = useLearnerDashboard(learnerId, days)
  const query = learnerId === undefined ? mine : learner
  const { data, isLoading, isError, error } = query

  const noAccess = isError && axios.isAxiosError(error) && error.response?.status === 403

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-3xl font-extrabold text-wb-ink">
          {learnerId === undefined ? 'My dashboard' : "Learner's dashboard"}
        </h1>
        <div className="flex gap-2" role="group" aria-label="Period">
          {RANGES.map((range) => (
            <button
              key={range}
              type="button"
              aria-pressed={days === range}
              onClick={() => setDays(range)}
              className={days === range ? primaryButtonClass : softButtonClass}
            >
              {range} days
            </button>
          ))}
        </div>
      </div>

      {isLoading && <p className="mt-6 text-lg text-wb-ink-muted">Loading the dashboard…</p>}

      {noAccess && (
        <div className="mt-6">
          <p className="text-lg text-wb-danger">You do not have access to this dashboard.</p>
          <Link to="/support" className="mt-2 inline-block text-wb-primary underline">
            Back to Supporters
          </Link>
        </div>
      )}

      {isError && !noAccess && (
        <p className="mt-6 text-lg text-wb-danger">Couldn't load the dashboard right now. Please try again later.</p>
      )}

      {data && (
        <div className="mt-6 flex flex-col gap-4">
          <p className="text-sm text-wb-ink-muted">
            {data.from} to {data.to}
          </p>
          <div className="grid gap-4 lg:grid-cols-2">
            <RetentionCard retention={data.retention} />
            <StreakCard streakDays={data.activity.currentStreakDays} weeks={data.activity.activeDaysPerWeek} />
          </div>
          <ActivityHeatmap days={data.activity.heatmap} />
          <div className="grid gap-4 lg:grid-cols-2">
            <DailyGoalBar goals={data.activity.dailyGoals} />
            <WordStatusBreakdown perStatus={data.words.perStatus} />
          </div>
          <WordsAddedChart added={days === 90 ? data.words.addedPerWeek : data.words.addedPerDay} unit={days === 90 ? 'week' : 'day'} />
          <div className="grid gap-4 lg:grid-cols-2">
            <StruggleList struggle={data.struggle} />
            <GamingSignals gaming={data.gaming} />
          </div>
        </div>
      )}
    </motion.div>
  )
}
