import type { SupportLinkStatus, SupportRelationship } from '../../types'

/** Shared Tailwind class strings for the support-link screens (wb- tokens only). */
export const inputClass =
  'w-full rounded-wb-md border-2 border-wb-border-control px-4 py-3 text-base focus:border-wb-primary'

export const primaryButtonClass =
  'rounded-wb-md bg-wb-primary px-4 py-2 text-sm font-bold text-wb-on-primary shadow-wb-card hover:bg-wb-primary-hover disabled:opacity-60'

export const successButtonClass =
  'rounded-wb-md bg-wb-success px-4 py-2 text-sm font-bold text-wb-on-success shadow-wb-card hover:bg-wb-success-hover disabled:opacity-60'

export const dangerButtonClass =
  'rounded-wb-md bg-wb-danger-soft px-4 py-2 text-sm font-bold text-wb-danger-soft-ink hover:bg-wb-danger-soft-hover disabled:opacity-60'

export const softButtonClass =
  'rounded-wb-md bg-wb-primary-soft px-4 py-2 text-sm font-bold text-wb-ink hover:bg-wb-primary-soft-hover disabled:opacity-60'

export const cardClass = 'rounded-wb-card bg-wb-surface-card p-5 shadow-wb-card'

export const RELATIONSHIPS: SupportRelationship[] = ['Parent', 'Teacher', 'Partner', 'Other']

export const statusLabel: Record<SupportLinkStatus, string> = {
  Invited: 'Invited',
  PendingPrimaryApproval: 'Waiting for Primary approval',
  Active: 'Active',
  Revoked: 'Ended',
}

/** Emoji per avatar id of the backend catalog. Unknown ids fall back to a neutral face. */
export const AVATAR_EMOJI: Record<string, string> = {
  fox: '🦊',
  owl: '🦉',
  cat: '🐱',
  dog: '🐶',
  panda: '🐼',
  lion: '🦁',
  rabbit: '🐰',
  turtle: '🐢',
  robot: '🤖',
  rocket: '🚀',
}

/** Emoji for an avatar id, or a neutral face. */
export function avatarEmoji(avatarId: string | null | undefined): string {
  return (avatarId ? AVATAR_EMOJI[avatarId] : undefined) ?? '🙂'
}

/** Local date of an ISO timestamp, for display. */
export function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString()
}
