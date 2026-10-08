import type { ReactElement } from 'react'
import { useState } from 'react'
import { problemMessage } from '../../api/supportLinks'
import {
  useCancelUnlink,
  useEscalateUnlink,
  useRequestUnlink,
  useRespondUnlink,
} from '../../hooks/useSupportLinks'
import type { SupportLink } from '../../types'
import {
  avatarEmoji,
  cardClass,
  dangerButtonClass,
  formatDate,
  softButtonClass,
  statusLabel,
  successButtonClass,
} from './supportUi'

type SupportLinkCardProps = Readonly<{
  link: SupportLink
  /** Which party to show as the counterpart. */
  show: 'supporter' | 'learner' | 'both'
  /** False for child callers: links are read-only (the Primary acts for the child). */
  canAct: boolean
}>

/** One support link with its unlink actions (request, confirm/decline, cancel, escalate). */
export function SupportLinkCard({ link, show, canAct }: SupportLinkCardProps): ReactElement {
  const requestUnlink = useRequestUnlink()
  const respondUnlink = useRespondUnlink()
  const cancelUnlink = useCancelUnlink()
  const escalateUnlink = useEscalateUnlink()

  // Time of first render; good enough to enable the button (the server re-checks the wait).
  const [renderedAt] = useState<number>(() => Date.now())
  const request = link.unlinkRequest
  const escalationOpen = request !== null && new Date(request.escalationAvailableAtUtc).getTime() <= renderedAt
  const busy = requestUnlink.isPending || respondUnlink.isPending || cancelUnlink.isPending || escalateUnlink.isPending
  const error = requestUnlink.error ?? respondUnlink.error ?? cancelUnlink.error ?? escalateUnlink.error

  const title =
    show === 'supporter'
      ? `${avatarEmoji(link.supporterAvatarId)} ${link.supporterName}`
      : show === 'learner'
        ? `${avatarEmoji(link.learnerAvatarId)} ${link.learnerName}`
        : `${link.supporterName} supports ${link.learnerName}`

  return (
    <div className={cardClass}>
      <div className="flex flex-wrap items-center gap-2">
        <p className="text-xl font-bold text-wb-ink">{title}</p>
        {link.isPrimary && (
          <span className="rounded-wb-md bg-wb-primary-soft px-2 py-1 text-xs font-bold text-wb-ink">Primary</span>
        )}
        {link.relationship && (
          <span className="rounded-wb-md bg-wb-hover-tint px-2 py-1 text-xs font-semibold text-wb-ink-muted">
            {link.relationship}
          </span>
        )}
        <span className="text-sm text-wb-ink-muted">{statusLabel[link.status]}</span>
      </div>

      {link.isPrimary && (
        <p className="mt-2 text-sm text-wb-ink-muted">The Primary link cannot be unlinked. An admin can hand Primary over.</p>
      )}

      {request && (
        <p className="mt-2 text-sm text-wb-ink-muted">
          {request.status === 'OverrideRequested'
            ? 'Unlink request sent to an admin.'
            : request.requestedByMe
              ? `You asked to unlink on ${formatDate(request.requestedAtUtc)}.`
              : `The other side asked to unlink on ${formatDate(request.requestedAtUtc)}.`}
        </p>
      )}

      {canAct && link.status === 'Active' && (
        <div className="mt-3 flex flex-wrap gap-2">
          {!request && !link.isPrimary && (
            <button
              type="button"
              disabled={busy}
              onClick={() => requestUnlink.mutate(link.id)}
              className={dangerButtonClass}
            >
              Unlink
            </button>
          )}

          {request?.status === 'Pending' && !request.requestedByMe && (
            <>
              <button
                type="button"
                disabled={busy}
                onClick={() => respondUnlink.mutate({ linkId: link.id, confirm: true })}
                className={successButtonClass}
              >
                Confirm unlink
              </button>
              <button
                type="button"
                disabled={busy}
                onClick={() => respondUnlink.mutate({ linkId: link.id, confirm: false })}
                className={dangerButtonClass}
              >
                Decline
              </button>
            </>
          )}

          {request?.status === 'Pending' && request.requestedByMe && (
            <>
              <button type="button" disabled={busy} onClick={() => cancelUnlink.mutate(link.id)} className={softButtonClass}>
                Cancel request
              </button>
              <button
                type="button"
                disabled={busy || !escalationOpen}
                onClick={() => escalateUnlink.mutate(link.id)}
                className={softButtonClass}
              >
                {escalationOpen ? 'Ask admin' : `Ask admin from ${formatDate(request.escalationAvailableAtUtc)}`}
              </button>
            </>
          )}
        </div>
      )}

      {error && <p className="mt-2 text-sm text-wb-danger">{problemMessage(error, 'That did not work. Please try again.')}</p>}
    </div>
  )
}
