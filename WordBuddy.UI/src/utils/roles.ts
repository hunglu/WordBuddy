import type { User } from '../types'

/** Whether the user is an admin. Display only; the server enforces the real rule. */
export function isAdmin(user: User | null | undefined): boolean {
  return user?.isAdmin === true
}

/**
 * Whether the user may see content work: an admin, or an adult moderator.
 * A child never counts, even with a stray moderator flag. Display only; the server enforces the real rule.
 */
export function canManageContent(user: User | null | undefined): boolean {
  if (user === null || user === undefined) {
    return false
  }
  return user.isAdmin === true || (user.isModerator === true && user.ageGroup === 'Adult')
}
