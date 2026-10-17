import type { ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import { useApproveChildWord, usePendingApprovals } from '../../hooks/useAutofill'
import { usePagination } from '../../hooks/usePagination'
import { Pager } from '../Pager'
import { SenseCard } from '../autofill/SenseCard'
import { successButtonClass } from './supportUi'

type WordsToApproveProps = Readonly<{
  learnerId: string
  learnerName: string
}>

/**
 * "Words to approve" for one supported learner: auto-filled words the child added. Approve makes the
 * word visible to this child only. Rendered only for supporters (never on a child account).
 */
export function WordsToApprove({ learnerId, learnerName }: WordsToApproveProps): ReactElement | null {
  const { t } = useTranslation('support')
  const pagination = usePagination()
  const { data: page, isLoading, isError } = usePendingApprovals(learnerId, pagination.params)
  pagination.clamp(page?.totalCount)
  const approve = useApproveChildWord(learnerId)

  if (isLoading) return null
  if (isError) {
    return <p className="mt-2 text-sm text-wb-ink-muted">{t('words.loadFailed')}</p>
  }
  if (!page || page.totalCount === 0) return null

  return (
    <section className="mt-3 flex flex-col gap-3" aria-label={t('words.ariaLabel', { name: learnerName })}>
      <h3 className="text-lg font-bold text-wb-ink">{t('words.heading')}</h3>
      <div className="grid gap-3 sm:grid-cols-2">
        {page.items.map((word, index) => (
          <SenseCard key={word.senseId} sense={word} index={index}>
            <button
              type="button"
              disabled={approve.isPending}
              onClick={() => approve.mutate(word.senseId)}
              className={successButtonClass}
            >
              {t('words.approve')}
            </button>
            {word.childSuitableHint === false && (
              <span className="self-center text-xs font-semibold text-wb-danger">{t('words.mayNotSuit')}</span>
            )}
          </SenseCard>
        ))}
      </div>
      <Pager
        page={pagination.page}
        pageSize={pagination.pageSize}
        totalCount={page.totalCount}
        onPageChange={pagination.setPage}
        label={t('words.pages', { name: learnerName })}
        itemLabel={t('words.items')}
      />
      {approve.isError && <p className="text-sm text-wb-danger">{t('words.approveFailed')}</p>}
    </section>
  )
}
