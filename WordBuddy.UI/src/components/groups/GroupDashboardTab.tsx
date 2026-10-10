import type { ReactElement } from 'react'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useGroupDashboard } from '../../hooks/useGroups'
import type { DashboardRange, LearnerGroupDetail } from '../../types'
import { avatarEmoji, cardClass, primaryButtonClass, softButtonClass } from '../support/supportUi'
import { NO_ACCESS_MESSAGE, inlineErrorClass, isForbidden } from './groupUi'

const RANGES: readonly DashboardRange[] = [7, 30, 90]

type GroupDashboardTabProps = Readonly<{ group: LearnerGroupDetail }>

function percent(value: number | null): string {
  return value === null ? '—' : `${value}%`
}

/** Dashboard tab: one row per active member (avatar and public name only), group totals, 7/30/90 day range. */
export function GroupDashboardTab({ group }: GroupDashboardTabProps): ReactElement {
  const [days, setDays] = useState<DashboardRange>(30)
  const activeMembers = group.members.filter((member) => member.status === 'Active')
  const { data, isLoading, isError, error } = useGroupDashboard(group.id, days, activeMembers.length > 0)
  const membersById = new Map(group.members.map((member) => [member.learnerId, member]))

  return (
    <section className={cardClass} aria-label="Group dashboard">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-xl font-bold text-wb-ink">Group dashboard</h2>
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

      {activeMembers.length === 0 && (
        <p className="mt-3 text-wb-ink-muted">The dashboard shows when the group has an active member.</p>
      )}
      {activeMembers.length > 0 && isLoading && <p className="mt-3 text-wb-ink-muted">Loading the dashboard…</p>}
      {isError && (
        <p className={`mt-3 ${inlineErrorClass}`}>
          {isForbidden(error) ? NO_ACCESS_MESSAGE : "Couldn't load the dashboard right now. Please try again later."}
        </p>
      )}

      {data && (
        <>
          <p className="mt-3 text-sm text-wb-ink-muted">
            {data.from} to {data.to} · {data.totals.memberCount} members · median retention{' '}
            {percent(data.totals.medianRetention)} · {data.totals.membersActiveThisWeek} active this week
          </p>
          <div className="mt-3 overflow-x-auto">
            <table className="w-full text-left text-sm text-wb-ink">
              <thead>
                <tr className="border-b border-wb-border-control">
                  <th className="py-2 pr-3">Learner</th>
                  <th className="py-2 pr-3">Streak</th>
                  <th className="py-2 pr-3">Active days</th>
                  <th className="py-2 pr-3">Retention</th>
                  <th className="py-2 pr-3">Leeches</th>
                  <th className="py-2 pr-3">Last active</th>
                  <th className="py-2 pr-3">
                    <span className="sr-only">Details</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {data.members.map((row) => {
                  const member = membersById.get(row.learnerId)
                  return (
                    <tr key={row.learnerId} className="border-b border-wb-border-control">
                      <td className="py-2 pr-3">
                        <span className="mr-2 text-xl" aria-hidden="true">
                          {avatarEmoji(member?.avatarId)}
                        </span>
                        <span className="font-semibold">{member?.publicName ?? 'Learner'}</span>
                      </td>
                      <td className="py-2 pr-3">{row.currentStreakDays}</td>
                      <td className="py-2 pr-3">{row.activeDays}</td>
                      <td className="py-2 pr-3">{percent(row.retention)}</td>
                      <td className="py-2 pr-3">{row.leechCount}</td>
                      <td className="py-2 pr-3">{row.lastActiveDate ?? '—'}</td>
                      <td className="py-2 pr-3">
                        <Link to={`/dashboard/learners/${row.learnerId}`} className="text-wb-primary underline">
                          Open
                        </Link>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        </>
      )}
    </section>
  )
}
