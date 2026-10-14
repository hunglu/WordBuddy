import { keepPreviousData, useMutation, useQuery, useQueryClient, type QueryClient } from '@tanstack/react-query'
import { getAdminLexeme, getAdminLexemes, removeLexemeAudio, updateLexeme, uploadLexemeAudio } from '../api/lexemes'
import type { AdminLexemeFilters } from '../types'

/** Refetches the admin lexeme views, and the learner and lesson views that show lexeme fields. */
function invalidateLexemes(queryClient: QueryClient): Promise<unknown[]> {
  return Promise.all([
    queryClient.invalidateQueries({ queryKey: ['adminLexemes'] }),
    queryClient.invalidateQueries({ queryKey: ['adminLexeme'] }),
    queryClient.invalidateQueries({ queryKey: ['vocabulary'] }),
    queryClient.invalidateQueries({ queryKey: ['lesson'] }),
    queryClient.invalidateQueries({ queryKey: ['adminLesson'] }),
  ])
}

/** One page of lexemes. Keeps the previous page on screen while the next one loads. */
export function useAdminLexemes(filters: AdminLexemeFilters, enabled: boolean) {
  return useQuery({
    queryKey: ['adminLexemes', filters],
    queryFn: () => getAdminLexemes(filters),
    enabled,
    placeholderData: keepPreviousData,
  })
}

export function useAdminLexeme(id: string | undefined, enabled: boolean) {
  return useQuery({
    queryKey: ['adminLexeme', id],
    queryFn: () => getAdminLexeme(id as string),
    enabled: enabled && id !== undefined,
    retry: false,
  })
}

export function useUpdateLexeme() {
  const queryClient = useQueryClient()
  return useMutation({ mutationFn: updateLexeme, onSuccess: () => invalidateLexemes(queryClient) })
}

export function useUploadLexemeAudio() {
  const queryClient = useQueryClient()
  return useMutation({ mutationFn: uploadLexemeAudio, onSuccess: () => invalidateLexemes(queryClient) })
}

export function useRemoveLexemeAudio() {
  const queryClient = useQueryClient()
  return useMutation({ mutationFn: removeLexemeAudio, onSuccess: () => invalidateLexemes(queryClient) })
}
