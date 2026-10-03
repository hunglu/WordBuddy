import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  addSharedWordToMyList,
  addVocabularyWord,
  deleteVocabularyWord,
  getMyVocabularyWords,
  getPendingModeration,
  getSharedVocabularyWords,
  moderateVocabularyWord,
  requestShareVocabularyWord,
  type ModerateVocabularyWordPayload,
} from '../api/vocabulary'
import { getVocabularyRecallProgress, submitVocabularyRecallCheck } from '../api/progress'
import type { VocabularyRecallResultItem } from '../types'

const MY_WORDS_KEY = ['vocabulary', 'mine']
const SHARED_WORDS_KEY = ['vocabulary', 'shared']
const PENDING_MODERATION_KEY = ['vocabulary', 'moderation', 'pending']
const RECALL_PROGRESS_KEY = ['progress', 'vocabulary-recall']

export function useMyVocabularyWords() {
  return useQuery({ queryKey: MY_WORDS_KEY, queryFn: getMyVocabularyWords })
}

export function useSharedVocabularyWords() {
  return useQuery({ queryKey: SHARED_WORDS_KEY, queryFn: getSharedVocabularyWords })
}

export function usePendingVocabularyModeration() {
  return useQuery({ queryKey: PENDING_MODERATION_KEY, queryFn: getPendingModeration })
}

export function useVocabularyRecallProgress() {
  return useQuery({ queryKey: RECALL_PROGRESS_KEY, queryFn: getVocabularyRecallProgress })
}

export function useAddVocabularyWord() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: addVocabularyWord,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: MY_WORDS_KEY }),
  })
}

export function useDeleteVocabularyWord() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: deleteVocabularyWord,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: MY_WORDS_KEY })
      // A confirmed delete of a shared word changes its owner in the pool.
      queryClient.invalidateQueries({ queryKey: SHARED_WORDS_KEY })
    },
  })
}

export function useRequestShareVocabularyWord() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: requestShareVocabularyWord,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: MY_WORDS_KEY }),
  })
}

export function useAddSharedWordToMyList() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: addSharedWordToMyList,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: MY_WORDS_KEY }),
  })
}

export function useModerateVocabularyWord() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: ModerateVocabularyWordPayload }) =>
      moderateVocabularyWord(id, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: PENDING_MODERATION_KEY })
      queryClient.invalidateQueries({ queryKey: SHARED_WORDS_KEY })
    },
  })
}

export function useSubmitVocabularyRecallCheck() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (results: VocabularyRecallResultItem[]) => submitVocabularyRecallCheck(results),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: RECALL_PROGRESS_KEY }),
  })
}
