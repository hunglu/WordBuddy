import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  addAutofillSense,
  approveAutofillForChildren,
  approveChildWord,
  getAdminPendingApprovals,
  getPendingApprovals,
  lookupAutofill,
} from '../api/vocabulary'

const MY_WORDS_KEY = ['vocabulary', 'mine']
const SHARED_WORDS_KEY = ['vocabulary', 'shared']
const ADMIN_PENDING_KEY = ['vocabulary', 'moderation', 'autofill-pending']

/**
 * Auto-fill lookup of `word`. Runs only once the learner clicks "Auto-fill" (the caller passes the
 * submitted word; `null` keeps the query idle). No retry: each call counts against the rate limit.
 */
export function useAutofillLookup(word: string | null) {
  return useQuery({
    queryKey: ['vocabulary', 'autofill', word],
    queryFn: () => lookupAutofill(word ?? ''),
    enabled: word !== null && word.trim().length > 0,
    retry: false,
    staleTime: 5 * 60 * 1000,
  })
}

/** Adds an auto-filled sense to my list. */
export function useAddAutofillSense() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: addAutofillSense,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: MY_WORDS_KEY }),
  })
}

/** Supporter queue of one learner. */
export function usePendingApprovals(learnerId: string) {
  return useQuery({
    queryKey: ['vocabulary', 'pending-approvals', learnerId],
    queryFn: () => getPendingApprovals(learnerId),
  })
}

/** Supporter approval of one word for one child. */
export function useApproveChildWord(learnerId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (senseId: string) => approveChildWord(learnerId, senseId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vocabulary', 'pending-approvals', learnerId] })
      queryClient.invalidateQueries({ queryKey: MY_WORDS_KEY })
    },
  })
}

/** Admin queue of auto-filled words waiting for child approval. */
export function useAdminPendingApprovals() {
  return useQuery({ queryKey: ADMIN_PENDING_KEY, queryFn: getAdminPendingApprovals })
}

/** Admin approval: the word becomes visible to every child. */
export function useApproveAutofillForChildren() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: approveAutofillForChildren,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ADMIN_PENDING_KEY })
      queryClient.invalidateQueries({ queryKey: SHARED_WORDS_KEY })
      queryClient.invalidateQueries({ queryKey: MY_WORDS_KEY })
    },
  })
}
