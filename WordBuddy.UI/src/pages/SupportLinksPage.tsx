import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { problemMessage } from '../api/supportLinks'
import { GroupRequests } from '../components/groups/GroupRequests'
import { InvitePanel } from '../components/support/InvitePanel'
import { SupportLinkCard } from '../components/support/SupportLinkCard'
import { WordsToApprove } from '../components/support/WordsToApprove'
import { cardClass, formatDate, formatPlainDate, primaryButtonClass, softButtonClass, successButtonClass, dangerButtonClass } from '../components/support/supportUi'
import {
  useCancelInvitation,
  usePendingSupporterApprovals,
  useRespondToPendingSupporter,
  useSupportLinks,
} from '../hooks/useSupportLinks'
import { ChallengeConsentToggle } from '../components/challenges/ChallengeConsentToggle'
import { useAuthStore } from '../store/authStore'
import { hasGuardianClaim } from '../utils/jwt'

/**
 * `/support` — learner support links. Adults: invite, accept, unlink, approve extra supporters of
 * children they are Primary for. Children: create an invitation and accept a code; links are read-only.
 */
export function SupportLinksPage(): ReactElement {
  const { t } = useTranslation('support')
  const user = useAuthStore((state) => state.user)
  const isChild = user?.ageGroup === 'Child'
  const isGuardian = useAuthStore((state) => hasGuardianClaim(state.token))

  const { data, isLoading, isError, error } = useSupportLinks()
  const pending = usePendingSupporterApprovals(!isChild)
  const respondPending = useRespondToPendingSupporter()
  const cancelInvitation = useCancelInvitation()

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <h1 className="text-3xl font-extrabold text-wb-ink">{t('title')}</h1>
      <p className="mt-2 text-wb-ink-muted">
        {isChild
          ? t('introChild')
          : t('introAdult')}
      </p>

      <div className="mt-6">
        <InvitePanel isChild={isChild} />
      </div>

      {isLoading && <p className="mt-8 text-lg text-wb-ink-muted">{t('loading')}</p>}
      {isError && (
        <p className="mt-8 text-lg text-wb-danger">{problemMessage(error, t('loadFailed'))}</p>
      )}

      {!isChild && pending.data && pending.data.length > 0 && (
        <section className="mt-8">
          <h2 className="text-2xl font-bold text-wb-ink">{t('pending.heading')}</h2>
          <p className="text-sm text-wb-ink-muted">{t('pending.hint')}</p>
          <div className="mt-3 flex flex-col gap-3">
            {pending.data.map((link) => (
              <div key={link.id} className={cardClass}>
                <p className="text-lg font-bold text-wb-ink">
                  {t('pending.wants', { supporter: link.supporterName, learner: link.learnerName })}
                </p>
                <div className="mt-3 flex gap-2">
                  <button
                    type="button"
                    disabled={respondPending.isPending}
                    onClick={() => respondPending.mutate({ linkId: link.id, approve: true })}
                    className={successButtonClass}
                  >
                    {t('pending.approve')}
                  </button>
                  <button
                    type="button"
                    disabled={respondPending.isPending}
                    onClick={() => respondPending.mutate({ linkId: link.id, approve: false })}
                    className={dangerButtonClass}
                  >
                    {t('pending.reject')}
                  </button>
                </div>
              </div>
            ))}
          </div>
          {respondPending.isError && (
            <p className="mt-2 text-sm text-wb-danger">{problemMessage(respondPending.error, t('pending.failed'))}</p>
          )}
        </section>
      )}
      {!isChild && pending.isError && (
        <p className="mt-4 text-sm text-wb-danger">{t('pending.loadFailed')}</p>
      )}

      {!isChild && <GroupRequests />}

      {data && (
        <>
          <section className="mt-8">
            <h2 className="text-2xl font-bold text-wb-ink">{t('lists.mySupporters')}</h2>
            {data.asLearner.length === 0 ? (
              <p className="mt-2 text-wb-ink-muted">{t('lists.noSupporters')}</p>
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
              <h2 className="text-2xl font-bold text-wb-ink">{t('lists.peopleISupport')}</h2>
              {data.asSupporter.length === 0 ? (
                <p className="mt-2 text-wb-ink-muted">{t('lists.nobody')}</p>
              ) : (
                <div className="mt-3 flex flex-col gap-3">
                  {data.asSupporter.map((link) => (
                    <div key={link.id}>
                      <SupportLinkCard link={link} show="learner" canAct />
                      {link.status === 'Active' && link.learnerDateOfBirth && (
                        <p className="mt-2 text-sm text-wb-ink-muted">
                          {t('lists.dob', { date: formatPlainDate(link.learnerDateOfBirth) })}
                        </p>
                      )}
                      {link.status === 'Active' && (
                        <Link to={`/dashboard/learners/${link.learnerId}`} className={`${primaryButtonClass} mt-2 inline-block`}>
                          {t('lists.viewDashboard')}
                        </Link>
                      )}
                      {link.status === 'Active' && isGuardian && (
                        <ChallengeConsentToggle childId={link.learnerId} childName={link.learnerName} />
                      )}
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
              <h2 className="text-2xl font-bold text-wb-ink">{t('lists.others')}</h2>
              <p className="text-sm text-wb-ink-muted">{t('lists.othersHint')}</p>
              <div className="mt-3 flex flex-col gap-3">
                {data.managedForChildren.map((link) => (
                  <SupportLinkCard key={link.id} link={link} show="both" canAct />
                ))}
              </div>
            </section>
          )}

          {data.openInvitations.length > 0 && (
            <section className="mt-8">
              <h2 className="text-2xl font-bold text-wb-ink">{t('lists.openInvitations')}</h2>
              <div className="mt-3 flex flex-col gap-3">
                {data.openInvitations.map((invitation) => (
                  <div key={invitation.id} className={`${cardClass} flex items-center justify-between gap-3`}>
                    <p className="text-wb-ink">
                      {t('lists.until', {
                        label: `${invitation.creatorSide === 'Learner' ? t('lists.lookingFor') : t('lists.offer')}${
                          invitation.relationship ? ` (${t(`relationship.${invitation.relationship}`)})` : ''
                        }`,
                        date: formatDate(invitation.expiresAtUtc),
                      })}
                    </p>
                    <button
                      type="button"
                      disabled={cancelInvitation.isPending}
                      onClick={() => cancelInvitation.mutate(invitation.id)}
                      className={softButtonClass}
                    >
                      {t('cancel', { ns: 'common' })}
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
