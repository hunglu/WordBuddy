import axios from 'axios'
import type { AddMemberOutcome, GroupMemberStatus } from '../../types'

/** Whether an API error is a 403 (not the owner / no access). */
export function isForbidden(error: unknown): boolean {
  return axios.isAxiosError(error) && error.response?.status === 403
}

export const memberStatusLabel: Record<GroupMemberStatus, string> = {
  PendingPrimaryApproval: 'Waiting for the Primary supporter',
  Active: 'Active',
  Removed: 'Removed',
}

export const addOutcomeLabel: Record<AddMemberOutcome, string> = {
  Added: 'added',
  PendingApproval: 'waiting for approval',
  Rejected: 'not added',
}

export const inlineErrorClass = 'text-sm text-wb-danger'

export const NO_ACCESS_MESSAGE = 'You do not have access to this group.'
