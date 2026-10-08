import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useState } from 'react'
import { Navigate } from 'react-router-dom'
import { problemMessage } from '../api/supportLinks'
import {
  cardClass,
  dangerButtonClass,
  formatDate,
  inputClass,
  primaryButtonClass,
  statusLabel,
  successButtonClass,
} from '../components/support/supportUi'
import {
  useAdminHandoverPrimary,
  useAdminLearnerSupportLinks,
  useAdminResolveUnlink,
  useAdminUnlinkRequests,
} from '../hooks/useSupportLinks'
import { useAuthStore } from '../store/authStore'

const REASON_MAX = 500

/** `/admin/support-links` — complete or reject escalated unlinks, hand Primary over. Admin only. */
export function AdminSupportLinksPage(): ReactElement {
  const user = useAuthStore((state) => state.user)
  const isAdmin = user?.isAdmin === true

  const requests = useAdminUnlinkRequests(isAdmin)
  const resolve = useAdminResolveUnlink()
  const handover = useAdminHandoverPrimary()
  const [reasonById, setReasonById] = useState<Record<string, string>>({})
  const [learnerId, setLearnerId] = useState('')
  const [lookupId, setLookupId] = useState('')
  const [newPrimaryLinkId, setNewPrimaryLinkId] = useState('')
  const [handoverReason, setHandoverReason] = useState('')
  const learnerLinks = useAdminLearnerSupportLinks(lookupId)

  // Server-side, `AdminOnly` already rejects a non-admin call — this is a UX guard.
  if (!isAdmin) {
    return <Navigate to="/" replace />
  }

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <h1 className="text-3xl font-extrabold text-wb-ink">Support links (admin)</h1>
      <p className="mt-2 text-wb-ink-muted">Every action is audited with your reason.</p>

      <section className="mt-6">
        <h2 className="text-2xl font-bold text-wb-ink">Escalated unlink requests</h2>
        {requests.isLoading && <p className="mt-3 text-wb-ink-muted">Loading…</p>}
        {requests.isError && <p className="mt-3 text-wb-danger">Couldn't load unlink requests right now.</p>}
        {requests.data && requests.data.length === 0 && <p className="mt-3 text-wb-ink-muted">Nothing waiting.</p>}

        <div className="mt-3 flex flex-col gap-3">
          {requests.data?.map((request) => {
            const reason = reasonById[request.id] ?? ''
            const canSubmit = reason.trim().length > 0 && !resolve.isPending
            return (
              <div key={request.id} className={cardClass}>
                <p className="text-sm text-wb-ink-muted">
                  Link {request.linkId} · learner {request.learnerId} · supporter {request.supporterId}
                </p>
                <p className="text-sm text-wb-ink-muted">
                  Requested by the {request.requestedBySide.toLowerCase()} side on {formatDate(request.requestedAtUtc)}
                  {request.escalatedAtUtc ? `, escalated on ${formatDate(request.escalatedAtUtc)}` : ''}
                </p>
                <label htmlFor={`reason-${request.id}`} className="mt-3 block text-sm font-semibold text-wb-ink">
                  Reason
                </label>
                <textarea
                  id={`reason-${request.id}`}
                  rows={2}
                  maxLength={REASON_MAX}
                  value={reason}
                  onChange={(e) => setReasonById((prev) => ({ ...prev, [request.id]: e.target.value }))}
                  className={inputClass}
                />
                <div className="mt-3 flex gap-2">
                  <button
                    type="button"
                    disabled={!canSubmit}
                    onClick={() => resolve.mutate({ id: request.id, reason: reason.trim(), complete: true })}
                    className={successButtonClass}
                  >
                    Complete unlink
                  </button>
                  <button
                    type="button"
                    disabled={!canSubmit}
                    onClick={() => resolve.mutate({ id: request.id, reason: reason.trim(), complete: false })}
                    className={dangerButtonClass}
                  >
                    Reject
                  </button>
                </div>
              </div>
            )
          })}
        </div>
        {resolve.isError && <p className="mt-2 text-sm text-wb-danger">{problemMessage(resolve.error, 'That did not work.')}</p>}
      </section>

      <section className="mt-10">
        <h2 className="text-2xl font-bold text-wb-ink">Primary handover</h2>
        <form
          className={`mt-3 ${cardClass}`}
          onSubmit={(e) => {
            e.preventDefault()
            setNewPrimaryLinkId('')
            setLookupId(learnerId.trim())
          }}
        >
          <label htmlFor="learnerId" className="block text-sm font-semibold text-wb-ink">
            Child learner id
          </label>
          <input id="learnerId" value={learnerId} onChange={(e) => setLearnerId(e.target.value)} className={inputClass} />
          <button type="submit" disabled={learnerId.trim().length === 0} className={`mt-3 ${primaryButtonClass}`}>
            Load links
          </button>
        </form>

        {learnerLinks.isError && <p className="mt-3 text-wb-danger">Couldn't load this learner's links.</p>}
        {learnerLinks.data && (
          <div className={`mt-3 ${cardClass}`}>
            {learnerLinks.data.length === 0 && <p className="text-wb-ink-muted">No links.</p>}
            <fieldset className="flex flex-col gap-2">
              <legend className="text-sm font-semibold text-wb-ink">New Primary</legend>
              {learnerLinks.data.map((link) => (
                <label key={link.id} className="flex items-center gap-2 text-sm text-wb-ink">
                  <input
                    type="radio"
                    name="newPrimary"
                    disabled={link.status !== 'Active' || link.isPrimary}
                    checked={newPrimaryLinkId === link.id}
                    onChange={() => setNewPrimaryLinkId(link.id)}
                  />
                  Supporter {link.supporterId} · {statusLabel[link.status]}
                  {link.isPrimary ? ' · current Primary' : ''}
                </label>
              ))}
            </fieldset>
            <label htmlFor="handoverReason" className="mt-3 block text-sm font-semibold text-wb-ink">
              Reason
            </label>
            <textarea
              id="handoverReason"
              rows={2}
              maxLength={REASON_MAX}
              value={handoverReason}
              onChange={(e) => setHandoverReason(e.target.value)}
              className={inputClass}
            />
            <button
              type="button"
              disabled={newPrimaryLinkId.length === 0 || handoverReason.trim().length === 0 || handover.isPending}
              onClick={() =>
                handover.mutate(
                  { learnerId: lookupId, newPrimaryLinkId, reason: handoverReason.trim() },
                  { onSuccess: () => setHandoverReason('') },
                )
              }
              className={`mt-3 ${primaryButtonClass}`}
            >
              Hand over Primary
            </button>
            {handover.isSuccess && <p className="mt-2 text-sm text-wb-ink">Primary handed over.</p>}
            {handover.isError && <p className="mt-2 text-sm text-wb-danger">{problemMessage(handover.error, 'Handover failed.')}</p>}
          </div>
        )}
      </section>
    </motion.div>
  )
}
