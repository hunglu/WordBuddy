import type {
  LearnerProgress,
  ReviewPayload,
  ReviewResult,
  VocabularyRecallProgress,
  VocabularyRecallResultItem,
  VocabularySession,
} from '../types'
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

const CLIENT_DATE_TIME_HEADER = 'X-Client-CurrentDateTime'

function pad(value: number): string {
  return String(value).padStart(2, '0')
}

/** Local time as ISO 8601 with the local offset, e.g. `2026-10-07T09:30:00+07:00`. */
export function toLocalIsoWithOffset(date: Date): string {
  const offsetMinutes = -date.getTimezoneOffset()
  const sign = offsetMinutes >= 0 ? '+' : '-'
  const absolute = Math.abs(offsetMinutes)
  return (
    `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}` +
    `T${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}` +
    `${sign}${pad(Math.floor(absolute / 60))}:${pad(absolute % 60)}`
  )
}

function clientDateTimeHeaders(): Record<string, string> {
  return { [CLIENT_DATE_TIME_HEADER]: toLocalIsoWithOffset(new Date()) }
}

export async function getVocabularySession(): Promise<VocabularySession> {
  const { data } = await apiClient.get<VocabularySession>('/progress/vocabulary/session', {
    headers: clientDateTimeHeaders(),
  })
  return data
}

export async function postVocabularyReview(payload: ReviewPayload): Promise<ReviewResult> {
  const { data } = await apiClient.post<ReviewResult>('/progress/vocabulary/reviews', payload, {
    headers: clientDateTimeHeaders(),
  })
  return data
}
