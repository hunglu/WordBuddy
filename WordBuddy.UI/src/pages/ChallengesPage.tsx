import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import { problemMessage } from '../api/supportLinks'
import { ChallengeCard } from '../components/challenges/ChallengeCard'
import { Pager } from '../components/Pager'
import { cardClass } from '../components/support/supportUi'
import { useChallenges } from '../hooks/useChallenges'
import { usePagination } from '../hooks/usePagination'
import { useAuthStore } from '../store/authStore'

/** `/challenges` — the open and running vocabulary challenges of the learner's audience. */
export function ChallengesPage(): ReactElement {
  const { t } = useTranslation('challenges')
  const user = useAuthStore((state) => state.user)
  const isChild = user?.ageGroup === 'Child'
  const pagination = usePagination()
  const { data, isLoading, isError, error } = useChallenges(pagination.params)
  pagination.clamp(data?.totalCount)

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <h1 className="text-3xl font-extrabold text-wb-ink">{t('title')}</h1>
      <p className="mt-2 text-wb-ink-muted">
        {t('intro')}
      </p>

      {isLoading && <p className="mt-6 text-lg text-wb-ink-muted">{t('loading')}</p>}
      {isError && (
        <p className="mt-6 text-lg text-wb-danger">{problemMessage(error, t('loadFailed'))}</p>
      )}

      {data && data.totalCount === 0 && (
        <div className={`mt-6 max-w-xl ${cardClass}`}>
          <p className="text-4xl">{isChild ? '🤝' : '🏆'}</p>
          {isChild ? (
            <>
              <p className="mt-2 text-lg font-bold text-wb-ink">{t('askGuardian')}</p>
              <p className="mt-1 text-wb-ink-muted">
                {t('askGuardianBody')}
              </p>
            </>
          ) : (
            <>
              <p className="mt-2 text-lg font-bold text-wb-ink">{t('noneTitle')}</p>
              <p className="mt-1 text-wb-ink-muted">{t('noneBody')}</p>
            </>
          )}
        </div>
      )}

      {data && data.items.length > 0 && (
        <div className="mt-6 flex max-w-2xl flex-col gap-3">
          {data.items.map((challenge) => (
            <ChallengeCard key={challenge.id} challenge={challenge} />
          ))}
        </div>
      )}

      {data && (
        <div className="max-w-2xl">
          <Pager
            page={pagination.page}
            pageSize={pagination.pageSize}
            totalCount={data.totalCount}
            onPageChange={pagination.setPage}
            label={t('pages')}
            itemLabel={t('items')}
          />
        </div>
      )}
    </motion.div>
  )
}
