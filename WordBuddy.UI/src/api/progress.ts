import type { LearnerProgress, VocabularyRecallProgress, VocabularyRecallResultItem } from '../types'
import { apiClient } from './client'

export interface RecordProgressPayload {
  lessonId: string
  isCompleted: boolean
  scorePercent?: number
}

export async function recordProgress(payload: RecordProgressPayload): Promise<void> {
  await apiClient.post('/progress', payload)
}

export async function getUserProgress(): Promise<LearnerProgress[]> {
  const { data } = await apiClient.get<LearnerProgress[]>('/progress')
  return data
}

export async function submitVocabularyRecallCheck(results: VocabularyRecallResultItem[]): Promise<void> {
  await apiClient.post('/progress/vocabulary-recall', { results })
}

export async function getVocabularyRecallProgress(): Promise<VocabularyRecallProgress> {
  const { data } = await apiClient.get<VocabularyRecallProgress>('/progress/vocabulary-recall')
  return data
}
