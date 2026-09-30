import type { Level, VocabularyShareStatus } from '../types'

/**
 * Chip classes per vocabulary share status. Built only from wb- tokens and written out in full
 * so Tailwind's scanner picks them up. `Record` makes a missing status a compile error.
 */
export const shareStatusClasses: Record<VocabularyShareStatus, string> = {
  Private: 'bg-wb-share-private-bg text-wb-share-private-ink border-wb-share-private-border',
  PendingReview: 'bg-wb-share-pending-bg text-wb-share-pending-ink border-wb-share-pending-border',
  Shared: 'bg-wb-share-shared-bg text-wb-share-shared-ink border-wb-share-shared-border',
  Rejected: 'bg-wb-share-rejected-bg text-wb-share-rejected-ink border-wb-share-rejected-border',
}

/**
 * Badge classes per lesson level. Built only from wb- tokens; `Record` makes a missing level a
 * compile error.
 */
export const levelClasses: Record<Level, string> = {
  Beginner: 'bg-wb-level-beginner-bg text-wb-level-beginner-ink',
  Intermediate: 'bg-wb-level-intermediate-bg text-wb-level-intermediate-ink',
  Advanced: 'bg-wb-level-advanced-bg text-wb-level-advanced-ink',
}
