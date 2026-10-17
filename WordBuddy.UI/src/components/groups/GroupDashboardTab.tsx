import type { ReactElement } from 'react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { MAX_PAGE_SIZE } from '../../api/paging'
import { useGroupDashboard, useGroupMembers } from '../../hooks/useGroups'
import type { DashboardRange, LearnerGroupDetail } from '../../types'
import { avatarEmoji, cardClass, primaryButtonClass, softButtonClass } from '../support/supportUi'
import { inlineErrorClass, isForbidden } from './groupUi'

const RANGES: readonly DashboardRange[] = [7, 30, 90]

type GroupDashboardTabProps = Readonly<{ group: LearnerGroupDetail }>

function percent(value: number | null): string {
  return value === null ? '—' : `${value}%`
}

/** Dashboard tab: one row per active member (avatar and public name only), group totals, 7/30/90 day range. */
export function GroupDashboardTab({ group }: GroupDashboardTabProps): ReactElement {
  const { t } = useTranslation('groups')
  const [days, setDays] = useState<DashboardRange>(30)
  const activeCount: number = group.activeMemberCount
  const { data, isLoading, isError, error } = useGroupDashboard(group.id, days, activeCount > 0)
  // A group has at most 40 members, so one call at the largest page size reads every name.
  const members = useGroupMembers(group.id, { page: 1, pageSize: MAX_PAGE_SIZE })
  const membersById = new Map((members.data?.items ?? []).map((member) => [member.learnerId, member]))

  return (
    <section className={cardClass} aria-label={t('dashboard.heading')}>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-xl font-bold text-wb-ink">{t('dashboard.heading')}</h2>
        <div className="flex gap-2" role="group" aria-label={t('dashboard.period')}>
          {RANGES.map((range) => (
            <button
              key={range}
              type="button"
              aria-pressed={days === range}
              onClick={() => setDays(range)}
              className={days === range ? primaryButtonClass : softButtonClass}
            >
              {t('dashboard.days', { count: range })}
            </button>
          ))}
        </div>
      </div>

      {activeCount === 0 && (
        <p className="mt-3 text-wb-ink-muted">{t('dashboard.needMember')}</p>
      )}
      {activeCount > 0 && isLoading && <p className="mt-3 text-wb-ink-muted">{t('dashboard.loading')}</p>}
      {isError && (
        <p className={`mt-3 ${inlineErrorClass}`}>
          {isForbidden(error) ? t('noAccess') : t('dashboard.loadFailed')}
        </p>
      )}

      {data && (
        <>
          <p className="mt-3 text-sm text-wb-ink-muted">
            {t('dashboard.summary', {
              from: data.from,
              to: data.to,
              members: data.totals.memberCount,
              retention: percent(data.totals.medianRetention),
              active: data.totals.membersActiveThisWeek,
            })}
          </p>
          <div className="mt-3 overflow-x-auto">
            <table className="w-full text-left text-sm text-wb-ink">
              <thead>
                <tr className="border-b border-wb-border-control">
                  <th className="py-2 pr-3">{t('dashboard.learner')}</th>
                  <th className="py-2 pr-3">{t('dashboard.streak')}</th>
                  <th className="py-2 pr-3">{t('dashboard.activeDays')}</th>
                  <th className="py-2 pr-3">{t('dashboard.retention')}</th>
                  <th className="py-2 pr-3">{t('dashboard.leeches')}</th>
                  <th className="py-2 pr-3">{t('dashboard.lastActive')}</th>
                  <th className="py-2 pr-3">
                    <span className="sr-only">{t('dashboard.details')}</span>
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
                        <span className="font-semibold">{member?.publicName ?? t('dashboard.unknownLearner')}</span>
                      </td>
                      <td className="py-2 pr-3">{row.currentStreakDays}</td>
                      <td className="py-2 pr-3">{row.activeDays}</td>
                      <td className="py-2 pr-3">{percent(row.retention)}</td>
                      <td className="py-2 pr-3">{row.leechCount}</td>
                      <td className="py-2 pr-3">{row.lastActiveDate ?? '—'}</td>
                      <td className="py-2 pr-3">
                        <Link to={`/dashboard/learners/${row.learnerId}`} className="text-wb-primary underline">
                          {t('dashboard.open')}
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
