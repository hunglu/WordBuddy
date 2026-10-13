import type { ReactElement } from 'react'

/** Status chip: published lessons are live for learners, drafts are admin-only. */
export function LessonStatusBadge({ isPublished }: Readonly<{ isPublished: boolean }>): ReactElement {
  return (
    <span
      className={`rounded-wb-pill border px-3 py-1 text-xs font-bold ${
        isPublished
          ? 'border-wb-share-shared-border bg-wb-share-shared-bg text-wb-share-shared-ink'
          : 'border-wb-share-pending-border bg-wb-share-pending-bg text-wb-share-pending-ink'
      }`}
    >
      {isPublished ? 'Published' : 'Draft'}
    </span>
  )
}
