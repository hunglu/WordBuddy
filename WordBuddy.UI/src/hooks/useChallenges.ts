import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  getChallengeBoard,
  joinChallenge,
  leaveChallenge,
  listChallenges,
  setChildChallengeConsent,
} from '../api/challenges'

export const CHALLENGES_KEY = ['challenges'] as const

/** Open and running challenges of the caller audience. */
export function useChallenges() {
  return useQuery({ queryKey: CHALLENGES_KEY, queryFn: listChallenges, retry: false })
}

/** The board of one challenge, filtered by the server for the caller. */
export function useChallengeBoard(id: string | undefined) {
  return useQuery({
    queryKey: [...CHALLENGES_KEY, 'board', id],
    queryFn: () => getChallengeBoard(id!), // runs only when `enabled` (id is defined)
    enabled: id !== undefined,
    retry: false,
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
