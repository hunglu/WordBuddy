import { useMutation, useQuery } from '@tanstack/react-query'
import { getVocabularySession, postVocabularyReview } from '../api/progress'
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

/** Visible senses for the session ids (Content filters hidden ones). */
export function useSenses(ids: string[]) {
  return useQuery({
    queryKey: ['senses', ids],
    queryFn: () => getSensesByIds(ids),
    enabled: ids.length > 0,
    refetchOnWindowFocus: false,
  })
}

/** Records one answer in Progress. */
export function useRecordReview() {
  return useMutation({
    mutationFn: (payload: ReviewPayload) => postVocabularyReview(payload),
  })
}
