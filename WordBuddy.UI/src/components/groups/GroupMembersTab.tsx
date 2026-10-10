import type { ReactElement } from 'react'
import { useState } from 'react'
import { problemMessage } from '../../api/supportLinks'
import { useAddGroupMembers, useRemoveGroupMember } from '../../hooks/useGroups'
import { useSupportLinks } from '../../hooks/useSupportLinks'
import type { AddMemberResult, LearnerGroupDetail } from '../../types'
import { avatarEmoji, cardClass, dangerButtonClass, primaryButtonClass } from '../support/supportUi'
import { addOutcomeLabel, inlineErrorClass, memberStatusLabel } from './groupUi'

type GroupMembersTabProps = Readonly<{ group: LearnerGroupDetail }>

/** Members of a group: list with pending badge and remove, plus adding learners the owner actively supports. */
export function GroupMembersTab({ group }: GroupMembersTabProps): ReactElement {
  const links = useSupportLinks()
  const addMembers = useAddGroupMembers()
  const removeMember = useRemoveGroupMember()
  const [selected, setSelected] = useState<string[]>([])
  const [results, setResults] = useState<AddMemberResult[]>([])

  const memberIds = new Set(group.members.map((member) => member.learnerId))
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
      <section className={cardClass} aria-label="Members">
        <h2 className="text-xl font-bold text-wb-ink">Members</h2>
        {group.members.length === 0 ? (
          <p className="mt-2 text-wb-ink-muted">No members yet. Add learners you support below.</p>
        ) : (
          <ul className="mt-3 flex flex-col gap-2">
            {group.members.map((member) => (
              <li key={member.memberId} className="flex flex-wrap items-center justify-between gap-2">
                <span className="flex items-center gap-2 text-wb-ink">
                  <span className="text-2xl" aria-hidden="true">
                    {avatarEmoji(member.avatarId)}
                  </span>
                  <span className="font-semibold">{member.publicName}</span>
                  {member.status === 'PendingPrimaryApproval' && (
                    <span className="rounded-wb-md bg-wb-primary-soft px-2 py-1 text-xs font-bold text-wb-ink">
                      {memberStatusLabel[member.status]}
                    </span>
                  )}
                </span>
                <button
                  type="button"
                  disabled={removeMember.isPending}
                  onClick={() => removeMember.mutate({ groupId: group.id, learnerId: member.learnerId })}
                  className={dangerButtonClass}
                >
                  Remove
                </button>
              </li>
            ))}
          </ul>
        )}
        {removeMember.isError && (
          <p className={`mt-2 ${inlineErrorClass}`}>{problemMessage(removeMember.error, 'Could not remove the member.')}</p>
        )}
      </section>

      <section className={cardClass} aria-label="Add members">
        <h2 className="text-xl font-bold text-wb-ink">Add members</h2>
        <p className="text-sm text-wb-ink-muted">
          You can add learners you actively support. A child needs the approval of the Primary supporter, unless that is you.
        </p>

        {links.isLoading && <p className="mt-2 text-wb-ink-muted">Loading the people you support…</p>}
        {links.isError && <p className={`mt-2 ${inlineErrorClass}`}>Couldn't load the people you support right now.</p>}

        {links.data && candidates.length === 0 && (
          <p className="mt-2 text-wb-ink-muted">Everyone you support is already in this group.</p>
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
              Add selected
            </button>
          </>
        )}

        {addMembers.isError && (
          <p className={`mt-2 ${inlineErrorClass}`}>{problemMessage(addMembers.error, 'Could not add the members.')}</p>
        )}
        {results.length > 0 && (
          <ul className="mt-3 text-sm text-wb-ink-muted" aria-label="Result of the last add">
            {results.map((result) => {
              const name = links.data?.asSupporter.find((link) => link.learnerId === result.learnerId)?.learnerName ?? 'Learner'
              return (
                <li key={result.learnerId}>
                  {name}: {addOutcomeLabel[result.outcome]}
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
