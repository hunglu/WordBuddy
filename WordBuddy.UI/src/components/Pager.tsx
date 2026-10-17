import { useTranslation } from 'react-i18next'
import { softButtonClass } from './support/supportUi'

interface PagerProps {
  /** Current 1-based page. */
  page: number
  pageSize: number
  totalCount: number
  onPageChange: (page: number) => void
  /** Accessible name of the navigation, e.g. "Lexeme pages". */
  label: string
  /** Translated plural noun after the count, e.g. "lexemes". Defaults to a generic "items". */
  itemLabel?: string
}

/** Previous / Next with "Page X of Y · N items". Renders nothing when the list is empty. */
export function Pager({ page, pageSize, totalCount, onPageChange, label, itemLabel }: PagerProps) {
  const { t } = useTranslation('common')
  if (totalCount <= 0) {
    return null
  }

  const totalPages: number = Math.max(1, Math.ceil(totalCount / pageSize))
  const current: number = Math.min(page, totalPages)

  return (
    <nav aria-label={label} className="mt-4 flex items-center justify-between gap-3">
      <button type="button" disabled={current <= 1} onClick={() => onPageChange(current - 1)} className={softButtonClass}>
        {t('pager.previous')}
      </button>
      <p className="text-sm font-semibold text-wb-ink">
        {t('pager.pageOf', { page: current, total: totalPages, count: totalCount, items: itemLabel ?? t('pager.items') })}
      </p>
      <button
        type="button"
        disabled={current >= totalPages}
        onClick={() => onPageChange(current + 1)}
        className={softButtonClass}
      >
        {t('pager.next')}
      </button>
    </nav>
  )
}
