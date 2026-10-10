import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  addGroupMembers,
  createGroup,
  deleteGroup,
  getGroup,
  getMyGroups,
  getPendingGroupApprovals,
  leaveGroup,
  removeGroupMember,
  renameGroup,
  respondToGroupMembership,
} from '../api/groups'
import { getGroupDashboard } from '../api/progress'
import { assignWordsToGroup, getGroupWordAssignments } from '../api/vocabulary'
import type { DashboardRange } from '../types'

export const GROUPS_KEY = ['groups'] as const
export const GROUP_APPROVALS_KEY = ['groups', 'approvals'] as const

/** Groups the caller owns and groups the caller joined. */
export function useMyGroups() {
  return useQuery({ queryKey: GROUPS_KEY, queryFn: getMyGroups })
}

/** One group with its members. Owner only (403 for anyone else). */
export function useGroup(groupId: string) {
  return useQuery({ queryKey: ['groups', groupId], queryFn: () => getGroup(groupId), retry: false })
}

/** Pending child memberships waiting for the caller (as Primary supporter). */
export function usePendingGroupApprovals(enabled = true) {
  return useQuery({ queryKey: GROUP_APPROVALS_KEY, queryFn: getPendingGroupApprovals, enabled })
}

/** Assignment history of a group. Owner only. */
export function useGroupWordAssignments(groupId: string) {
  return useQuery({
    queryKey: ['groupWords', groupId],
    queryFn: () => getGroupWordAssignments(groupId),
    retry: false,
  })
}

/** Dashboard of a group. Pass `enabled = false` while the group has no active members. */
export function useGroupDashboard(groupId: string, days: DashboardRange, enabled = true) {
  return useQuery({
    queryKey: ['dashboard', 'group', groupId, days],
    queryFn: () => getGroupDashboard(groupId, days),
    enabled,
    retry: false,
  })
}

/** Invalidates the group list, the group detail and the approvals. */
function useInvalidateGroups(): (groupId?: string) => Promise<void> {
  const queryClient = useQueryClient()
  return async (groupId?: string) => {
    await queryClient.invalidateQueries({ queryKey: GROUPS_KEY })
    if (groupId !== undefined) {
      await queryClient.invalidateQueries({ queryKey: ['dashboard', 'group', groupId] })
    }
  }
}

export function useCreateGroup() {
  const invalidate = useInvalidateGroups()
  return useMutation({ mutationFn: createGroup, onSuccess: () => invalidate() })
}

export function useRenameGroup() {
  const invalidate = useInvalidateGroups()
  return useMutation({ mutationFn: renameGroup, onSuccess: (_data, vars) => invalidate(vars.groupId) })
}

export function useDeleteGroup() {
  const invalidate = useInvalidateGroups()
  return useMutation({ mutationFn: deleteGroup, onSuccess: (_data, groupId) => invalidate(groupId) })
}

export function useAddGroupMembers() {
  const invalidate = useInvalidateGroups()
  return useMutation({ mutationFn: addGroupMembers, onSuccess: (_data, vars) => invalidate(vars.groupId) })
}

export function useRemoveGroupMember() {
  const invalidate = useInvalidateGroups()
  return useMutation({ mutationFn: removeGroupMember, onSuccess: (_data, vars) => invalidate(vars.groupId) })
}

export function useLeaveGroup() {
  const invalidate = useInvalidateGroups()
  return useMutation({ mutationFn: leaveGroup, onSuccess: () => invalidate() })
}

export function useRespondToGroupMembership() {
  const invalidate = useInvalidateGroups()
  return useMutation({
    mutationFn: ({ memberId, approve }: { memberId: string; approve: boolean }) =>
      respondToGroupMembership(memberId, approve),
    onSuccess: () => invalidate(),
  })
}

/** Assigns words to every active member. A new assignment shows in the history and adds words to the learners' lists. */
export function useAssignWordsToGroup() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: assignWordsToGroup,
    onSuccess: async (_data, vars) => {
      await queryClient.invalidateQueries({ queryKey: ['groupWords', vars.groupId] })
      await queryClient.invalidateQueries({ queryKey: ['dashboard', 'group', vars.groupId] })
    },
  })
}
