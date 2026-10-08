import axios from 'axios'
import type {
  AdminSupportLink,
  AdminUnlinkRequest,
  CreatedInvitation,
  InvitationSide,
  MySupportLinks,
  SupportLink,
  SupportRelationship,
  User,
} from '../types'
import { apiClient } from './client'

// ── Learner / supporter ─────────────────────────────────────────────────────

export interface CreateInvitationPayload {
  inviteAs: InvitationSide
  relationship: SupportRelationship | null
}

export interface AcceptInvitationPayload {
  code?: string
  token?: string
}

export async function getMySupportLinks(): Promise<MySupportLinks> {
  const { data } = await apiClient.get<MySupportLinks>('/auth/support-links')
  return data
}

export async function getPendingSupporterApprovals(): Promise<SupportLink[]> {
  const { data } = await apiClient.get<SupportLink[]>('/auth/support-links/pending-approvals')
  return data
}

export async function createInvitation(payload: CreateInvitationPayload): Promise<CreatedInvitation> {
  const { data } = await apiClient.post<CreatedInvitation>('/auth/support-links/invitations', payload)
  return data
}

export async function cancelInvitation(id: string): Promise<void> {
  await apiClient.delete(`/auth/support-links/invitations/${id}`)
}

export async function acceptInvitation(payload: AcceptInvitationPayload): Promise<SupportLink> {
  const { data } = await apiClient.post<SupportLink>('/auth/support-links/accept', payload)
  return data
}

export async function respondToPendingSupporter(linkId: string, approve: boolean): Promise<void> {
  await apiClient.post(`/auth/support-links/${linkId}/${approve ? 'approve' : 'reject'}`)
}

export async function requestUnlink(linkId: string): Promise<void> {
  await apiClient.post(`/auth/support-links/${linkId}/unlink-request`)
}

export async function respondUnlink(linkId: string, confirm: boolean): Promise<void> {
  await apiClient.post(`/auth/support-links/${linkId}/unlink-request/${confirm ? 'confirm' : 'decline'}`)
}

export async function cancelUnlink(linkId: string): Promise<void> {
  await apiClient.delete(`/auth/support-links/${linkId}/unlink-request`)
}

export async function escalateUnlink(linkId: string): Promise<void> {
  await apiClient.post(`/auth/support-links/${linkId}/unlink-request/escalate`)
}

// ── Admin ───────────────────────────────────────────────────────────────────

export async function getAdminUnlinkRequests(): Promise<AdminUnlinkRequest[]> {
  const { data } = await apiClient.get<AdminUnlinkRequest[]>('/auth/admin/unlink-requests')
  return data
}

export async function adminCompleteUnlink(id: string, reason: string): Promise<void> {
  await apiClient.post(`/auth/admin/unlink-requests/${id}/complete`, { reason })
}

export async function adminRejectUnlink(id: string, reason: string): Promise<void> {
  await apiClient.post(`/auth/admin/unlink-requests/${id}/reject`, { reason })
}

export async function getAdminLearnerSupportLinks(learnerId: string): Promise<AdminSupportLink[]> {
  const { data } = await apiClient.get<AdminSupportLink[]>('/auth/admin/support-links', { params: { learnerId } })
  return data
}

export interface HandoverPrimaryPayload {
  learnerId: string
  newPrimaryLinkId: string
  reason: string
}

export async function adminHandoverPrimary(payload: HandoverPrimaryPayload): Promise<void> {
  await apiClient.post('/auth/admin/support-links/handover', payload)
}

// ── Profile ─────────────────────────────────────────────────────────────────

export interface UpdateProfilePayload {
  alias: string | null
  avatarId: string | null
}

export async function getCurrentUser(): Promise<User> {
  const { data } = await apiClient.get<User>('/auth/me')
  return data
}

export async function updateProfile(payload: UpdateProfilePayload): Promise<User> {
  const { data } = await apiClient.put<User>('/auth/profile', payload)
  return data
}

export async function getAvatarCatalog(): Promise<string[]> {
  const { data } = await apiClient.get<string[]>('/auth/avatars')
  return data
}

// ── Errors ──────────────────────────────────────────────────────────────────

/** Problem title the Content/Progress child gate returns with 403. */
export const SUPPORTER_REQUIRED_CODE = 'Learner.SupporterRequired'

interface ProblemBody {
  title?: unknown
  detail?: unknown
}

function problemOf(error: unknown): ProblemBody | null {
  if (!axios.isAxiosError(error)) {
    return null
  }
  const body: unknown = error.response?.data
  return typeof body === 'object' && body !== null ? (body as ProblemBody) : null
}

/** Whether an API error is the child gate (403 `Learner.SupporterRequired`). */
export function isSupporterRequiredError(error: unknown): boolean {
  return (
    axios.isAxiosError(error) &&
    error.response?.status === 403 &&
    problemOf(error)?.title === SUPPORTER_REQUIRED_CODE
  )
}

/** Returns the problem code (title) of an API error, if any. */
export function problemCode(error: unknown): string | null {
  const title = problemOf(error)?.title
  return typeof title === 'string' ? title : null
}

/** Returns a short, user-facing message for an API error. */
export function problemMessage(error: unknown, fallback: string): string {
  const detail = problemOf(error)?.detail
  return typeof detail === 'string' && detail.length > 0 ? detail : fallback
}
