import type { AdminUser, PageParams, PagedResult } from '../types'
import { apiClient } from './client'

export interface AdminUsersParams extends PageParams {
  /** Matches email or display name. */
  search?: string
}

/** One page of adult accounts (admin only). */
export async function getAdminUsers(params: AdminUsersParams): Promise<PagedResult<AdminUser>> {
  const { data } = await apiClient.get<PagedResult<AdminUser>>('/auth/admin/users', { params })
  return data
}

export interface SetModeratorPayload {
  id: string
  isModerator: boolean
}

/** Grants or removes the Moderator role. It shows in the user's token after the next sign-in. */
export async function setModeratorRole({ id, isModerator }: SetModeratorPayload): Promise<void> {
  await apiClient.put(`/auth/admin/users/${id}/moderator`, { isModerator })
}
