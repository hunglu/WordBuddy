import type { PersonalVocabularyWord, VocabularyRecallCheckWord } from '../types'
import { apiClient } from './client'

export interface AddVocabularyWordPayload {
  word: string
  definition: string
  example?: string
}

export async function addVocabularyWord(payload: AddVocabularyWordPayload): Promise<string> {
  const { data } = await apiClient.post<string>('/vocabulary', payload)
  return data
}

export async function getMyVocabularyWords(): Promise<PersonalVocabularyWord[]> {
  const { data } = await apiClient.get<PersonalVocabularyWord[]>('/vocabulary/mine')
  return data
}

export async function requestShareVocabularyWord(id: string): Promise<void> {
  await apiClient.post(`/vocabulary/${id}/share`)
}

export interface DeleteVocabularyWordPayload {
  id: string
  /** Required by the API when the author deletes a `Shared` or `PendingReview` word (else 409). */
  confirm?: boolean
}

export async function deleteVocabularyWord({ id, confirm }: DeleteVocabularyWordPayload): Promise<void> {
  await apiClient.delete(`/vocabulary/${id}`, { params: confirm ? { confirm: true } : undefined })
}

export async function getSharedVocabularyWords(): Promise<PersonalVocabularyWord[]> {
  const { data } = await apiClient.get<PersonalVocabularyWord[]>('/vocabulary/shared')
  return data
}

export async function addSharedWordToMyList(id: string): Promise<string> {
  const { data } = await apiClient.post<string>(`/vocabulary/shared/${id}/add-to-mine`)
  return data
}

export async function getRandomWordsForCheck(count: number): Promise<VocabularyRecallCheckWord[]> {
  const { data } = await apiClient.get<VocabularyRecallCheckWord[]>('/vocabulary/check', { params: { count } })
  return data
}

export async function getPendingModeration(): Promise<PersonalVocabularyWord[]> {
  const { data } = await apiClient.get<PersonalVocabularyWord[]>('/vocabulary/moderation/pending')
  return data
}

export interface ModerateVocabularyWordPayload {
  approve: boolean
  visibleToChildren: boolean
}

export async function moderateVocabularyWord(id: string, payload: ModerateVocabularyWordPayload): Promise<void> {
  await apiClient.post(`/vocabulary/moderation/${id}`, payload)
}
