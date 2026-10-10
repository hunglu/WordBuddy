import type { ReactElement } from 'react'
import { problemMessage } from '../../api/supportLinks'
import { usePendingGroupApprovals, useRespondToGroupMembership } from '../../hooks/useGroups'
import { avatarEmoji, cardClass, dangerButtonClass, successButtonClass } from '../support/supportUi'
import { inlineErrorClass } from './groupUi'

/** "Group requests" for the Primary supporter of a child: approve or reject a child joining a group. */
export function GroupRequests(): ReactElement | null {
  const pending = usePendingGroupApprovals()
  const respond = useRespondToGroupMembership()

  if (pending.isError) {
    return <p className={`mt-4 ${inlineErrorClass}`}>Couldn't load group requests right now.</p>
  }

  if (!pending.data || pending.data.length === 0) {
    return null
  }

  return (
    <section className="mt-8" aria-label="Group requests">
      <h2 className="text-2xl font-bold text-wb-ink">Group requests</h2>
      <p className="text-sm text-wb-ink-muted">A supporter wants to add your child to a group. You decide as Primary supporter.</p>
      <div className="mt-3 flex flex-col gap-3">
        {pending.data.map((request) => (
          <div key={request.memberId} className={cardClass}>
            <p className="text-lg font-bold text-wb-ink">
              <span aria-hidden="true">{avatarEmoji(request.learnerAvatarId)} </span>
              {request.ownerName} wants to add {request.learnerName} to a group
            </p>
            <div className="mt-3 flex gap-2">
              <button
                type="button"
                disabled={respond.isPending}
                onClick={() => respond.mutate({ memberId: request.memberId, approve: true })}
                className={successButtonClass}
              >
                Approve
              </button>
              <button
                type="button"
                disabled={respond.isPending}
                onClick={() => respond.mutate({ memberId: request.memberId, approve: false })}
                className={dangerButtonClass}
              >
                Reject
              </button>
            </div>
          </div>
        ))}
      </div>
      {respond.isError && <p className={`mt-2 ${inlineErrorClass}`}>{problemMessage(respond.error, 'That did not work.')}</p>}
    </section>
  )
}
