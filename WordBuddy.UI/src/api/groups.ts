import type {
  AddMemberResult,
  GroupMember,
  JoinedGroup,
  LearnerGroupDetail,
  OwnedGroup,
  PageParams,
  PagedResult,
  PendingGroupApproval,
} from '../types'
import { apiClient } from './client'

/** One page of the groups the caller owns. */
export async function getOwnedGroups(paging: PageParams): Promise<PagedResult<OwnedGroup>> {
  const { data } = await apiClient.get<PagedResult<OwnedGroup>>('/auth/groups/owned', { params: paging })
  return data
}

/** One page of the groups the caller joined (name and owner only). */
export async function getJoinedGroups(paging: PageParams): Promise<PagedResult<JoinedGroup>> {
  const { data } = await apiClient.get<PagedResult<JoinedGroup>>('/auth/groups/joined', { params: paging })
  return data
}

/** One page of the pending and active members of a group. Owner only. */
export async function getGroupMembers(groupId: string, paging: PageParams): Promise<PagedResult<GroupMember>> {
  const { data } = await apiClient.get<PagedResult<GroupMember>>(`/auth/groups/${groupId}/members`, { params: paging })
  return data
}

export async function getGroup(groupId: string): Promise<LearnerGroupDetail> {
  const { data } = await apiClient.get<LearnerGroupDetail>(`/auth/groups/${groupId}`)
  return data
}

/** Creates a group and returns its id. */
export async function createGroup(name: string): Promise<string> {
  const { data } = await apiClient.post<{ id: string }>('/auth/groups', { name })
  return data.id
}

export interface RenameGroupPayload {
  groupId: string
  name: string
}

export async function renameGroup({ groupId, name }: RenameGroupPayload): Promise<void> {
  await apiClient.put(`/auth/groups/${groupId}`, { name })
}

export async function deleteGroup(groupId: string): Promise<void> {
  await apiClient.delete(`/auth/groups/${groupId}`)
}

export interface AddGroupMembersPayload {
  groupId: string
  learnerIds: string[]
}

/** One outcome per learner: added, waiting for the Primary supporter, or rejected with a code. */
export async function addGroupMembers({ groupId, learnerIds }: AddGroupMembersPayload): Promise<AddMemberResult[]> {
  const { data } = await apiClient.post<AddMemberResult[]>(`/auth/groups/${groupId}/members`, { learnerIds })
  return data
}

export interface RemoveGroupMemberPayload {
  groupId: string
  learnerId: string
}

export async function removeGroupMember({ groupId, learnerId }: RemoveGroupMemberPayload): Promise<void> {
  await apiClient.delete(`/auth/groups/${groupId}/members/${learnerId}`)
}

/** An adult learner leaves a group. A child gets 403 (the Primary supporter removes the child). */
export async function leaveGroup(groupId: string): Promise<void> {
  await apiClient.post(`/auth/groups/${groupId}/leave`)
}

/** Pending child memberships waiting for the caller (as Primary supporter). */
export async function getPendingGroupApprovals(paging: PageParams): Promise<PagedResult<PendingGroupApproval>> {
  const { data } = await apiClient.get<PagedResult<PendingGroupApproval>>('/auth/groups/approvals', { params: paging })
  return data
}

export async function respondToGroupMembership(memberId: string, approve: boolean): Promise<void> {
  await apiClient.post(`/auth/groups/approvals/${memberId}/${approve ? 'approve' : 'reject'}`)
}
