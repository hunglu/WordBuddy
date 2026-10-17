import type { ReactElement } from 'react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { problemMessage } from '../../api/supportLinks'
import { useAddGroupMembers, useGroupMembers, useRemoveGroupMember } from '../../hooks/useGroups'
import { usePagination } from '../../hooks/usePagination'
import { useSupportLinks } from '../../hooks/useSupportLinks'
import type { AddMemberResult, LearnerGroupDetail } from '../../types'
import { Pager } from '../Pager'
import { avatarEmoji, cardClass, dangerButtonClass, primaryButtonClass } from '../support/supportUi'
import { inlineErrorClass } from './groupUi'

type GroupMembersTabProps = Readonly<{ group: LearnerGroupDetail }>

/** Members of a group: list with pending badge and remove, plus adding learners the owner actively supports. */
export function GroupMembersTab({ group }: GroupMembersTabProps): ReactElement {
  const { t } = useTranslation('groups')
  const links = useSupportLinks()
  const pagination = usePagination()
  const members = useGroupMembers(group.id, pagination.params)
  pagination.clamp(members.data?.totalCount)
  const addMembers = useAddGroupMembers()
  const removeMember = useRemoveGroupMember()
  const [selected, setSelected] = useState<string[]>([])
  const [results, setResults] = useState<AddMemberResult[]>([])

  const memberIds = new Set(group.memberLearnerIds)
  const candidates = (links.data?.asSupporter ?? []).filter(
    (link) => link.status === 'Active' && !memberIds.has(link.learnerId),
  )

  function toggle(learnerId: string): void {
    setSelected((current) =>
      current.includes(learnerId) ? current.filter((id) => id !== learnerId) : [...current, learnerId],
    )
  }

  function submit(): void {
    addMembers.mutate(
      { groupId: group.id, learnerIds: selected },
      {
        onSuccess: (outcomes) => {
          setResults(outcomes)
          setSelected([])
        },
      },
    )
  }

  return (
    <div className="flex flex-col gap-6">
      <section className={cardClass} aria-label={t('members.heading')}>
        <h2 className="text-xl font-bold text-wb-ink">{t('members.heading')}</h2>
        {members.isLoading && <p className="mt-2 text-wb-ink-muted">{t('members.loading')}</p>}
        {members.isError && <p className={`mt-2 ${inlineErrorClass}`}>{t('members.loadFailed')}</p>}
        {members.data && members.data.totalCount === 0 && (
          <p className="mt-2 text-wb-ink-muted">{t('members.none')}</p>
        )}
        {members.data && members.data.items.length > 0 && (
          <ul className="mt-3 flex flex-col gap-2">
            {members.data.items.map((member) => (
              <li key={member.memberId} className="flex flex-wrap items-center justify-between gap-2">
                <span className="flex items-center gap-2 text-wb-ink">
                  <span className="text-2xl" aria-hidden="true">
                    {avatarEmoji(member.avatarId)}
                  </span>
                  <span className="font-semibold">{member.publicName}</span>
                  {member.status === 'PendingPrimaryApproval' && (
                    <span className="rounded-wb-md bg-wb-primary-soft px-2 py-1 text-xs font-bold text-wb-ink">
                      {t(`memberStatus.${member.status}`)}
                    </span>
                  )}
                </span>
                <button
                  type="button"
                  disabled={removeMember.isPending}
                  onClick={() => removeMember.mutate({ groupId: group.id, learnerId: member.learnerId })}
                  className={dangerButtonClass}
                >
                  {t('members.remove')}
                </button>
              </li>
            ))}
          </ul>
        )}
        {members.data && (
          <Pager
            page={pagination.page}
            pageSize={pagination.pageSize}
            totalCount={members.data.totalCount}
            onPageChange={pagination.setPage}
            label={t('members.pages')}
            itemLabel={t('members.items')}
          />
        )}
        {removeMember.isError && (
          <p className={`mt-2 ${inlineErrorClass}`}>{problemMessage(removeMember.error, t('members.removeFailed'))}</p>
        )}
      </section>

      <section className={cardClass} aria-label={t('members.addHeading')}>
        <h2 className="text-xl font-bold text-wb-ink">{t('members.addHeading')}</h2>
        <p className="text-sm text-wb-ink-muted">
          {t('members.addHint')}
        </p>

        {links.isLoading && <p className="mt-2 text-wb-ink-muted">{t('members.candidatesLoading')}</p>}
        {links.isError && <p className={`mt-2 ${inlineErrorClass}`}>{t('members.candidatesFailed')}</p>}

        {links.data && candidates.length === 0 && (
          <p className="mt-2 text-wb-ink-muted">{t('members.everyoneIn')}</p>
        )}

        {candidates.length > 0 && (
          <>
            <ul className="mt-3 flex flex-col gap-2">
              {candidates.map((link) => (
                <li key={link.id}>
                  <label className="flex cursor-pointer items-center gap-2 text-wb-ink">
                    <input
                      type="checkbox"
                      checked={selected.includes(link.learnerId)}
                      onChange={() => toggle(link.learnerId)}
                    />
                    <span className="text-2xl" aria-hidden="true">
                      {avatarEmoji(link.learnerAvatarId)}
                    </span>
                    <span className="font-semibold">{link.learnerName}</span>
                  </label>
                </li>
              ))}
            </ul>
            <button
              type="button"
              disabled={selected.length === 0 || addMembers.isPending}
              onClick={submit}
              className={`${primaryButtonClass} mt-3`}
            >
              {t('members.addSelected')}
            </button>
          </>
        )}

        {addMembers.isError && (
          <p className={`mt-2 ${inlineErrorClass}`}>{problemMessage(addMembers.error, t('members.addFailed'))}</p>
        )}
        {results.length > 0 && (
          <ul className="mt-3 text-sm text-wb-ink-muted" aria-label={t('members.lastAdd')}>
            {results.map((result) => {
              const name = links.data?.asSupporter.find((link) => link.learnerId === result.learnerId)?.learnerName ?? t('dashboard.unknownLearner')
              return (
                <li key={result.learnerId}>
                  {name}: {t(`addOutcome.${result.outcome}`)}
                  {result.errorCode ? ` (${result.errorCode})` : ''}
                </li>
              )
            })}
          </ul>
        )}
      </section>
    </div>
  )
}
