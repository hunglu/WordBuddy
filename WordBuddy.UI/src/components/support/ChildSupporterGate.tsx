import { motion } from 'framer-motion'
import type { ReactElement, ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { useCurrentUser } from '../../hooks/useSupportLinks'
import { useAuthStore } from '../../store/authStore'
import { cardClass, primaryButtonClass } from './supportUi'

type ChildSupporterGateProps = Readonly<{ children: ReactNode }>

/**
 * Wraps learning routes. A child without an active supporter sees the "Add a supporter" screen
 * instead (fresh `hasActiveSupporter` from `/api/auth/me`; a 403 `Learner.SupporterRequired`
 * from Content/Progress refetches it). Adults and admins pass straight through. The backend
 * policy is the real enforcement; this is the UX.
 */
export function ChildSupporterGate({ children }: ChildSupporterGateProps): ReactElement {
  const { t } = useTranslation('common')
  const user = useAuthStore((state) => state.user)
  const gated = user?.ageGroup === 'Child' && !user.isAdmin
  const currentUser = useCurrentUser(gated)

  if (!gated) {
    return <>{children}</>
  }

  if (currentUser.isLoading) {
    return <p className="text-lg text-wb-ink-muted">{t('loading')}</p>
  }

  // On an Identity error, let the page load: the backend gate still applies and the page shows its own error.
  if (currentUser.data && currentUser.data.hasActiveSupporter !== true) {
    return <AddSupporterScreen />
  }

  return <>{children}</>
}

/** Shown to a child who has no active supporter yet. */
function AddSupporterScreen(): ReactElement {
  const { t } = useTranslation('common')
  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <div className={`mx-auto mt-10 max-w-xl text-center ${cardClass}`}>
        <p className="text-5xl">🤝</p>
        <h1 className="mt-3 text-3xl font-extrabold text-wb-ink">{t('supporterGate.title')}</h1>
        <p className="mt-2 text-wb-ink-muted">
          {t('supporterGate.body')}
        </p>
        <Link to="/support" className={`mt-6 inline-block ${primaryButtonClass}`}>
          {t('supporterGate.action')}
        </Link>
      </div>
    </motion.div>
  )
}
