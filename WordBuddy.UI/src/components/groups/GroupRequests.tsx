import type { ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import { problemMessage } from '../../api/supportLinks'
import { usePendingGroupApprovals, useRespondToGroupMembership } from '../../hooks/useGroups'
import { usePagination } from '../../hooks/usePagination'
import { Pager } from '../Pager'
import { avatarEmoji, cardClass, dangerButtonClass, successButtonClass } from '../support/supportUi'
import { inlineErrorClass } from './groupUi'

/** "Group requests" for the Primary supporter of a child: approve or reject a child joining a group. */
export function GroupRequests(): ReactElement | null {
  const { t } = useTranslation('groups')
  const pagination = usePagination()
  const pending = usePendingGroupApprovals(pagination.params)
  pagination.clamp(pending.data?.totalCount)
  const respond = useRespondToGroupMembership()

  if (pending.isError) {
    return <p className={`mt-4 ${inlineErrorClass}`}>{t('requests.loadFailed')}</p>
  }

  if (!pending.data || pending.data.totalCount === 0) {
    return null
  }

  return (
    <section className="mt-8" aria-label={t('requests.heading')}>
      <h2 className="text-2xl font-bold text-wb-ink">{t('requests.heading')}</h2>
      <p className="text-sm text-wb-ink-muted">{t('requests.hint')}</p>
      <div className="mt-3 flex flex-col gap-3">
        {pending.data.items.map((request) => (
          <div key={request.memberId} className={cardClass}>
            <p className="text-lg font-bold text-wb-ink">
              <span aria-hidden="true">{avatarEmoji(request.learnerAvatarId)} </span>
              {t('requests.wants', { owner: request.ownerName, learner: request.learnerName })}
            </p>
            <div className="mt-3 flex gap-2">
              <button
                type="button"
                disabled={respond.isPending}
                onClick={() => respond.mutate({ memberId: request.memberId, approve: true })}
                className={successButtonClass}
              >
                {t('requests.approve')}
              </button>
              <button
                type="button"
                disabled={respond.isPending}
                onClick={() => respond.mutate({ memberId: request.memberId, approve: false })}
                className={dangerButtonClass}
              >
                {t('requests.reject')}
              </button>
            </div>
          </div>
        ))}
      </div>
      <Pager
        page={pagination.page}
        pageSize={pagination.pageSize}
        totalCount={pending.data.totalCount}
        onPageChange={pagination.setPage}
        label={t('requests.pages')}
        itemLabel={t('requests.items')}
      />
      {respond.isError && <p className={`mt-2 ${inlineErrorClass}`}>{problemMessage(respond.error, t('requests.failed'))}</p>}
    </section>
  )
}
