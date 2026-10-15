import { useCallback, useState } from 'react'
import { DEFAULT_PAGE_SIZE } from '../api/paging'
import type { PageParams } from '../types'

export interface Pagination {
  /** Current 1-based page. */
  page: number
  pageSize: number
  /** Ready to pass to a paged API call and to put at the end of a query key. */
  params: PageParams
  setPage: (page: number) => void
  /** Back to page 1. Call it when a filter or a search text changes. */
  reset: () => void
  /**
   * Keeps the page inside the list. Call it during render with the latest `totalCount` of the query:
   * when a delete or an edit empties the current page, it moves to the last page that still has items.
   */
  clamp: (totalCount: number | undefined) => void
}

/** Page state of one paged list. The page lives in component state, not in the URL. */
export function usePagination(pageSize: number = DEFAULT_PAGE_SIZE): Pagination {
  const [page, setPageState] = useState<number>(1)

  const setPage = useCallback((next: number): void => setPageState(Math.max(1, next)), [])
  const reset = useCallback((): void => setPageState(1), [])

  const clamp = (totalCount: number | undefined): void => {
    if (totalCount === undefined) {
      return
    }
    const lastPage: number = Math.max(1, Math.ceil(totalCount / pageSize))
    if (page > lastPage) {
      setPageState(lastPage)
    }
  }

  return { page, pageSize, params: { page, pageSize }, setPage, reset, clamp }
}
