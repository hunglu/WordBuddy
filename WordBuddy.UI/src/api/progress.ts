import type { LearnerProgress } from '../types'
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
