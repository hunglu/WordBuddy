import type { ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import { useAdminPendingApprovals, useApproveAutofillForChildren } from '../../hooks/useAutofill'
import { usePagination } from '../../hooks/usePagination'
import { Pager } from '../Pager'
import { SenseCard } from './SenseCard'

/**
 * Admin "Auto-filled words" tab: auto-filled words that children added and wait on. Approve makes
 * the word visible to every child. The page itself is admin-only (`AdminOnly` on the API).
 */
export function AutofillApprovalsPanel(): ReactElement {
  const { t } = useTranslation('vocabulary')
  const pagination = usePagination()
  const { data: page, isLoading, isError } = useAdminPendingApprovals(pagination.params)
  pagination.clamp(page?.totalCount)
  const approve = useApproveAutofillForChildren()

  if (isLoading) return <p className="mt-8 text-lg text-wb-ink-muted">{t('autofill.approvalsLoading')}</p>
  if (isError) return <p className="mt-8 text-lg text-wb-danger">{t('autofill.approvalsFailed')}</p>
  if (!page || page.totalCount === 0) {
    return <p className="mt-8 text-lg text-wb-ink-muted">{t('autofill.approvalsNone')}</p>
  }

  return (
    <div className="mt-6 flex flex-col gap-4">
      <p className="text-wb-ink-muted">{t('autofill.approvalsHint')}</p>
      <div className="grid gap-4 sm:grid-cols-2">
        {page.items.map((word, index) => (
          <SenseCard key={word.senseId} sense={word} index={index}>
            <button
              type="button"
              disabled={approve.isPending}
              onClick={() => approve.mutate(word.senseId)}
              className="rounded-wb-md bg-wb-success px-4 py-2 text-sm font-bold text-wb-on-success shadow-wb-card hover:bg-wb-success-hover disabled:opacity-60"
            >
              {t('autofill.approve')}
            </button>
            {word.childSuitableHint === false && (
              <span className="self-center text-xs font-semibold text-wb-danger">{t('autofill.flagged')}</span>
            )}
          </SenseCard>
        ))}
      </div>
      <Pager
        page={pagination.page}
        pageSize={pagination.pageSize}
        totalCount={page.totalCount}
        onPageChange={pagination.setPage}
        label={t('autofill.pages')}
        itemLabel={t('autofill.items')}
      />
      {approve.isError && <p className="text-sm text-wb-danger">{t('autofill.approveFailed')}</p>}
    </div>
  )
}
