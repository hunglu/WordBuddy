import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { Link, useParams } from 'react-router-dom'
import { problemCode, problemMessage } from '../api/supportLinks'
import { cardClass, primaryButtonClass } from '../components/support/supportUi'
import { useAcceptInvitation } from '../hooks/useSupportLinks'

const EXPIRED_CODE = 'SupportLink.InvitationExpired'

/** `/support/accept/:token` — accept an invitation from a shared link. */
export function AcceptInvitationPage(): ReactElement {
  const { token = '' } = useParams()
  const accept = useAcceptInvitation()
  const expired = accept.isError && problemCode(accept.error) === EXPIRED_CODE

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <h1 className="text-3xl font-extrabold text-wb-ink">Support invitation</h1>

      <div className={`mt-6 max-w-xl ${cardClass}`}>
        {accept.isSuccess ? (
          <>
            <p className="text-lg text-wb-ink">
              {accept.data.status === 'PendingPrimaryApproval'
                ? 'Accepted. The Primary supporter must approve before the link is active.'
                : 'Accepted. The link is active.'}
            </p>
            <Link to="/support" className={`mt-4 inline-block ${primaryButtonClass}`}>
              Go to Supporters
            </Link>
          </>
        ) : (
          <>
            <p className="text-wb-ink">Someone invited you to a support link. Accept it?</p>
            <button
              type="button"
              disabled={accept.isPending || token.length === 0}
              onClick={() => accept.mutate({ token })}
              className={`mt-4 ${primaryButtonClass}`}
            >
              Accept invitation
            </button>
          </>
        )}

        {expired && <p className="mt-3 text-wb-danger">This invitation has expired. Ask for a new one.</p>}
        {accept.isError && !expired && (
          <p className="mt-3 text-wb-danger">{problemMessage(accept.error, 'Could not accept this invitation.')}</p>
        )}
      </div>
    </motion.div>
  )
}
