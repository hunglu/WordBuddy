import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { problemMessage } from '../api/supportLinks'
import { InvitePanel } from '../components/support/InvitePanel'
import { SupportLinkCard } from '../components/support/SupportLinkCard'
import { WordsToApprove } from '../components/support/WordsToApprove'
import { cardClass, formatDate, softButtonClass, successButtonClass, dangerButtonClass } from '../components/support/supportUi'
import {
  useCancelInvitation,
  usePendingSupporterApprovals,
  useRespondToPendingSupporter,
  useSupportLinks,
} from '../hooks/useSupportLinks'
import { useAuthStore } from '../store/authStore'

/**
 * `/support` — learner support links. Adults: invite, accept, unlink, approve extra supporters of
 * children they are Primary for. Children: create an invitation and accept a code; links are read-only.
 */
export function SupportLinksPage(): ReactElement {
  const user = useAuthStore((state) => state.user)
  const isChild = user?.ageGroup === 'Child'

  const { data, isLoading, isError, error } = useSupportLinks()
  const pending = usePendingSupporterApprovals(!isChild)
  const respondPending = useRespondToPendingSupporter()
  const cancelInvitation = useCancelInvitation()

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <h1 className="text-3xl font-extrabold text-wb-ink">Supporters</h1>
      <p className="mt-2 text-wb-ink-muted">
        {isChild
          ? 'Ask a grown-up to support you. Share your code with them.'
          : 'A supporter can see progress, assign words and set the daily new-word cap.'}
      </p>

      <div className="mt-6">
        <InvitePanel isChild={isChild} />
      </div>

      {isLoading && <p className="mt-8 text-lg text-wb-ink-muted">Loading your links…</p>}
      {isError && (
        <p className="mt-8 text-lg text-wb-danger">{problemMessage(error, "Couldn't load your links right now.")}</p>
      )}

      {!isChild && pending.data && pending.data.length > 0 && (
        <section className="mt-8">
          <h2 className="text-2xl font-bold text-wb-ink">Waiting for your approval</h2>
          <p className="text-sm text-wb-ink-muted">You are the Primary supporter. Approve or reject extra supporters.</p>
          <div className="mt-3 flex flex-col gap-3">
            {pending.data.map((link) => (
              <div key={link.id} className={cardClass}>
                <p className="text-lg font-bold text-wb-ink">
                  {link.supporterName} wants to support {link.learnerName}
                </p>
                <div className="mt-3 flex gap-2">
                  <button
                    type="button"
                    disabled={respondPending.isPending}
                    onClick={() => respondPending.mutate({ linkId: link.id, approve: true })}
                    className={successButtonClass}
                  >
                    Approve
                  </button>
                  <button
                    type="button"
                    disabled={respondPending.isPending}
                    onClick={() => respondPending.mutate({ linkId: link.id, approve: false })}
                    className={dangerButtonClass}
                  >
                    Reject
                  </button>
                </div>
              </div>
            ))}
          </div>
          {respondPending.isError && (
            <p className="mt-2 text-sm text-wb-danger">{problemMessage(respondPending.error, 'That did not work.')}</p>
          )}
        </section>
      )}
      {!isChild && pending.isError && (
        <p className="mt-4 text-sm text-wb-danger">Couldn't load pending approvals right now.</p>
      )}

      {data && (
        <>
          <section className="mt-8">
            <h2 className="text-2xl font-bold text-wb-ink">My supporters</h2>
            {data.asLearner.length === 0 ? (
              <p className="mt-2 text-wb-ink-muted">No supporters yet.</p>
            ) : (
              <div className="mt-3 flex flex-col gap-3">
                {data.asLearner.map((link) => (
                  <SupportLinkCard key={link.id} link={link} show="supporter" canAct={!isChild} />
                ))}
              </div>
            )}
          </section>

          {!isChild && (
            <section className="mt-8">
              <h2 className="text-2xl font-bold text-wb-ink">People I support</h2>
              {data.asSupporter.length === 0 ? (
                <p className="mt-2 text-wb-ink-muted">You do not support anyone yet.</p>
              ) : (
                <div className="mt-3 flex flex-col gap-3">
                  {data.asSupporter.map((link) => (
                    <div key={link.id}>
                      <SupportLinkCard link={link} show="learner" canAct />
                      {link.status === 'Active' && (
                        <WordsToApprove learnerId={link.learnerId} learnerName={link.learnerName} />
                      )}
                    </div>
                  ))}
                </div>
              )}
            </section>
          )}

          {!isChild && data.managedForChildren.length > 0 && (
            <section className="mt-8">
              <h2 className="text-2xl font-bold text-wb-ink">Other supporters of my children</h2>
              <p className="text-sm text-wb-ink-muted">As Primary, you act for the child on these links.</p>
              <div className="mt-3 flex flex-col gap-3">
                {data.managedForChildren.map((link) => (
                  <SupportLinkCard key={link.id} link={link} show="both" canAct />
                ))}
              </div>
            </section>
          )}

          {data.openInvitations.length > 0 && (
            <section className="mt-8">
              <h2 className="text-2xl font-bold text-wb-ink">Open invitations</h2>
              <div className="mt-3 flex flex-col gap-3">
                {data.openInvitations.map((invitation) => (
                  <div key={invitation.id} className={`${cardClass} flex items-center justify-between gap-3`}>
                    <p className="text-wb-ink">
                      {invitation.creatorSide === 'Learner' ? 'Looking for a supporter' : 'Offer to support'}
                      {invitation.relationship ? ` (${invitation.relationship})` : ''} · until {formatDate(invitation.expiresAtUtc)}
                    </p>
                    <button
                      type="button"
                      disabled={cancelInvitation.isPending}
                      onClick={() => cancelInvitation.mutate(invitation.id)}
                      className={softButtonClass}
                    >
                      Cancel
                    </button>
                  </div>
                ))}
              </div>
            </section>
          )}
        </>
      )}
    </motion.div>
  )
}
