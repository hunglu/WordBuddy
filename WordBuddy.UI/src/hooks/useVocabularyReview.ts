import { useMutation, useQuery } from '@tanstack/react-query'
import { getVocabularySession, postVocabularyExercise, postVocabularyReview } from '../api/progress'
import { getSensesByIds } from '../api/vocabulary'
import type { ReviewPayload } from '../types'

/** Today's SRS session. Always fresh; not refetched on focus so a running session is not reset. */
export function useVocabularySession() {
  return useQuery({
    queryKey: ['vocabulary-session'],
    queryFn: getVocabularySession,
    staleTime: 0,
    refetchOnWindowFocus: false,
  })
}

/** Visible senses for the given ids (Content filters hidden ones). Used by the dashboard, not by the review flow. */
export function useSenses(ids: string[]) {
  return useQuery({
    queryKey: ['senses', ids],
    queryFn: () => getSensesByIds(ids),
    enabled: ids.length > 0,
    refetchOnWindowFocus: false,
  })
}

/** Asks the server to build the next exercise for one session word. */
export function useCreateExercise() {
  return useMutation({
    mutationFn: ({ sessionId, senseId }: { sessionId: string; senseId: string }) =>
      postVocabularyExercise(sessionId, senseId),
  })
}

/** Sends the raw answer to Progress, which grades it. */
export function useRecordReview() {
  return useMutation({
    mutationFn: (payload: ReviewPayload) => postVocabularyReview(payload),
  })
}
