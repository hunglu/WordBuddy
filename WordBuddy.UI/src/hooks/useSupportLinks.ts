import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  acceptInvitation,
  adminCompleteUnlink,
  adminHandoverPrimary,
  adminRejectUnlink,
  cancelInvitation,
  cancelUnlink,
  createInvitation,
  escalateUnlink,
  getAdminLearnerSupportLinks,
  getAdminUnlinkRequests,
  getAvatarCatalog,
  getCurrentUser,
  getMySupportLinks,
  getPendingSupporterApprovals,
  requestUnlink,
  respondToPendingSupporter,
  respondUnlink,
  updateProfile,
  type HandoverPrimaryPayload,
} from '../api/supportLinks'

export const SUPPORT_LINKS_KEY = ['supportLinks'] as const
export const PENDING_APPROVALS_KEY = ['supportLinks', 'pending'] as const
export const ADMIN_UNLINK_REQUESTS_KEY = ['adminUnlinkRequests'] as const
export const CURRENT_USER_KEY = ['currentUser'] as const

/** Invalidates everything a link change can affect (links, approvals, the caller's supporter flag). */
function useInvalidateLinks(): () => Promise<void> {
  const queryClient = useQueryClient()
  return async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: SUPPORT_LINKS_KEY }),
      queryClient.invalidateQueries({ queryKey: CURRENT_USER_KEY }),
    ])
  }
}

/** The caller's links (as learner, as supporter, managed as Primary) and open invitations. */
export function useSupportLinks() {
  return useQuery({ queryKey: SUPPORT_LINKS_KEY, queryFn: getMySupportLinks })
}

/** Extra supporters waiting for the caller (as Primary) to approve. */
export function usePendingSupporterApprovals(enabled = true) {
  return useQuery({ queryKey: PENDING_APPROVALS_KEY, queryFn: getPendingSupporterApprovals, enabled })
}

/** The caller's fresh profile, incl. `hasActiveSupporter` for the child gate. */
export function useCurrentUser(enabled = true) {
  return useQuery({ queryKey: CURRENT_USER_KEY, queryFn: getCurrentUser, enabled })
}

export function useAvatarCatalog() {
  return useQuery({ queryKey: ['avatars'], queryFn: getAvatarCatalog, staleTime: Infinity })
}

export function useCreateInvitation() {
  const invalidate = useInvalidateLinks()
  return useMutation({ mutationFn: createInvitation, onSuccess: invalidate })
}

export function useCancelInvitation() {
  const invalidate = useInvalidateLinks()
  return useMutation({ mutationFn: cancelInvitation, onSuccess: invalidate })
}

export function useAcceptInvitation() {
  const invalidate = useInvalidateLinks()
  return useMutation({ mutationFn: acceptInvitation, onSuccess: invalidate })
}

export function useRespondToPendingSupporter() {
  const invalidate = useInvalidateLinks()
  return useMutation({
    mutationFn: ({ linkId, approve }: { linkId: string; approve: boolean }) => respondToPendingSupporter(linkId, approve),
    onSuccess: invalidate,
  })
}

export function useRequestUnlink() {
  const invalidate = useInvalidateLinks()
  return useMutation({ mutationFn: requestUnlink, onSuccess: invalidate })
}

export function useRespondUnlink() {
  const invalidate = useInvalidateLinks()
  return useMutation({
    mutationFn: ({ linkId, confirm }: { linkId: string; confirm: boolean }) => respondUnlink(linkId, confirm),
    onSuccess: invalidate,
  })
}

export function useCancelUnlink() {
  const invalidate = useInvalidateLinks()
  return useMutation({ mutationFn: cancelUnlink, onSuccess: invalidate })
}

export function useEscalateUnlink() {
  const invalidate = useInvalidateLinks()
  return useMutation({ mutationFn: escalateUnlink, onSuccess: invalidate })
}

export function useUpdateProfile() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: updateProfile,
    onSuccess: (user) => {
      queryClient.setQueryData(CURRENT_USER_KEY, user)
      void queryClient.invalidateQueries({ queryKey: SUPPORT_LINKS_KEY })
    },
  })
}

// ── Admin ───────────────────────────────────────────────────────────────────

export function useAdminUnlinkRequests(enabled = true) {
  return useQuery({ queryKey: ADMIN_UNLINK_REQUESTS_KEY, queryFn: getAdminUnlinkRequests, enabled })
}

export function useAdminLearnerSupportLinks(learnerId: string) {
  return useQuery({
    queryKey: ['adminSupportLinks', learnerId],
    queryFn: () => getAdminLearnerSupportLinks(learnerId),
    enabled: learnerId.length > 0,
  })
}

export function useAdminResolveUnlink() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, reason, complete }: { id: string; reason: string; complete: boolean }) =>
      complete ? adminCompleteUnlink(id, reason) : adminRejectUnlink(id, reason),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ADMIN_UNLINK_REQUESTS_KEY }),
  })
}

export function useAdminHandoverPrimary() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (payload: HandoverPrimaryPayload) => adminHandoverPrimary(payload),
    onSuccess: (_data, payload) =>
      queryClient.invalidateQueries({ queryKey: ['adminSupportLinks', payload.learnerId] }),
  })
}
