import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  getChallengeBoard,
  joinChallenge,
  leaveChallenge,
  listChallenges,
  setChildChallengeConsent,
} from '../api/challenges'
import type { PageParams } from '../types'

export const CHALLENGES_KEY = ['challenges'] as const

/** One page of the open and running challenges of the caller audience. */
export function useChallenges(paging: PageParams) {
  return useQuery({
    queryKey: [...CHALLENGES_KEY, 'list', paging],
    queryFn: () => listChallenges(paging),
    retry: false,
    placeholderData: keepPreviousData,
  })
}

/** One page of the board of one challenge, filtered by the server for the caller. */
export function useChallengeBoard(id: string | undefined, paging: PageParams) {
  return useQuery({
    queryKey: [...CHALLENGES_KEY, 'board', id, paging],
    queryFn: () => getChallengeBoard(id!, paging), // runs only when `enabled` (id is defined)
    enabled: id !== undefined,
    retry: false,
    placeholderData: keepPreviousData,
  })
}

/** Joins a challenge, then refreshes the list and every board. */
export function useJoinChallenge() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: joinChallenge,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: CHALLENGES_KEY }),
  })
}

export function useLeaveChallenge() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: leaveChallenge,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: CHALLENGES_KEY }),
  })
}

/** A Guardian allows or stops challenges for a child. */
export function useSetChildChallengeConsent() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ childId, enabled }: { childId: string; enabled: boolean }) =>
      setChildChallengeConsent(childId, enabled),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: CHALLENGES_KEY }),
  })
}
