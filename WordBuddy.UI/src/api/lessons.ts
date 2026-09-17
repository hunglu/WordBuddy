import type { Lesson, LessonDetail, LessonFilters } from '../types'
import { apiClient } from './client'

export async function getLessons(filters?: LessonFilters): Promise<Lesson[]> {
  const { data } = await apiClient.get<Lesson[]>('/lessons', { params: filters })
  return data
}

export async function getLessonDetail(id: string): Promise<LessonDetail> {
  const { data } = await apiClient.get<LessonDetail>(`/lessons/${id}`)
  return data
}
