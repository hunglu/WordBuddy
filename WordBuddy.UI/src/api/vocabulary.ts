import type {
  AutofillResult,
  ChildApproval,
  PersonalVocabularyWord,
  SenseReview,
  VocabularyRecallCheckWord,
} from '../types'
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

/** Max ids per `GET /vocabulary/senses` call (server rule). */
const SENSES_CHUNK_SIZE = 100

/**
 * Loads senses by id, in chunks of 100, and merges the replies. Hidden and unknown ids are
 * simply missing from the result.
 */
export async function getSensesByIds(ids: string[]): Promise<SenseReview[]> {
  const chunks: string[][] = []
  for (let i = 0; i < ids.length; i += SENSES_CHUNK_SIZE) {
    chunks.push(ids.slice(i, i + SENSES_CHUNK_SIZE))
  }

  const replies: SenseReview[][] = await Promise.all(
    chunks.map(async (chunk) => {
      const params = new URLSearchParams()
      chunk.forEach((id) => params.append('ids', id))
      const { data } = await apiClient.get<SenseReview[]>('/vocabulary/senses', { params })
      return data
    }),
  )
  return replies.flat()
}

// ── Auto-fill (WB-25) ───────────────────────────────────────────────────────

/** Auto-fills a typed word. Only the word is sent. Rate limited (10/min). */
export async function lookupAutofill(word: string): Promise<AutofillResult> {
  const { data } = await apiClient.get<AutofillResult>('/vocabulary/autofill', { params: { word } })
  return data
}

/** Adds an auto-filled sense to my list. Idempotent; returns the sense id. */
export async function addAutofillSense(senseId: string): Promise<string> {
  const { data } = await apiClient.post<string>(`/vocabulary/autofill/${senseId}/add-to-mine`)
  return data
}

/** Supporter queue: auto-filled words a learner waits on. */
export async function getPendingApprovals(learnerId: string): Promise<ChildApproval[]> {
  const { data } = await apiClient.get<ChildApproval[]>(`/vocabulary/learners/${learnerId}/pending-approvals`)
  return data
}

/** Supporter approval: visible to this child only. */
export async function approveChildWord(learnerId: string, senseId: string): Promise<void> {
  await apiClient.post(`/vocabulary/learners/${learnerId}/words/${senseId}/approve`)
}

/** Admin queue: auto-filled words any child waits on. */
export async function getAdminPendingApprovals(): Promise<ChildApproval[]> {
  const { data } = await apiClient.get<ChildApproval[]>('/vocabulary/moderation/autofill-pending')
  return data
}

/** Admin approval: visible to every child. */
export async function approveAutofillForChildren(senseId: string): Promise<void> {
  await apiClient.post(`/vocabulary/moderation/autofill/${senseId}/approve`)
}
