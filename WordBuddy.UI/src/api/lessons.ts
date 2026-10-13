import type {
  AdminLessonDetailDto,
  AdminLessonDto,
  AdminLessonFilters,
  AdminSenseSearchResult,
  Lesson,
  LessonDetail,
  LessonFilters,
  LessonFormValues,
} from '../types'
import { apiClient } from './client'

export async function getLessons(filters?: LessonFilters): Promise<Lesson[]> {
  const { data } = await apiClient.get<Lesson[]>('/lessons', { params: filters })
  return data
}

export async function getLessonDetail(id: string): Promise<LessonDetail> {
  const { data } = await apiClient.get<LessonDetail>(`/lessons/${id}`)
  return data
}

// ---- Admin lesson authoring (AdminOnly on the server)

export async function getAdminLessons(filters?: AdminLessonFilters): Promise<AdminLessonDto[]> {
  const { data } = await apiClient.get<AdminLessonDto[]>('/lessons/admin', { params: filters })
  return data
}

export async function getAdminLessonDetail(id: string): Promise<AdminLessonDetailDto> {
  const { data } = await apiClient.get<AdminLessonDetailDto>(`/lessons/admin/${id}`)
  return data
}

export async function searchLessonSenses(search: string): Promise<AdminSenseSearchResult[]> {
  const { data } = await apiClient.get<AdminSenseSearchResult[]>('/lessons/admin/senses', { params: { search } })
  return data
}

/** Creates a draft lesson and returns its id. */
export async function createLesson(values: LessonFormValues): Promise<string> {
  const { data } = await apiClient.post<string>('/lessons/admin', values)
  return data
}

export interface UpdateLessonPayload {
  id: string
  values: Omit<LessonFormValues, 'type'>
}

export async function updateLesson({ id, values }: UpdateLessonPayload): Promise<void> {
  await apiClient.put(`/lessons/admin/${id}`, values)
}

export async function publishLesson(id: string): Promise<void> {
  await apiClient.post(`/lessons/admin/${id}/publish`)
}

export async function unpublishLesson(id: string): Promise<void> {
  await apiClient.post(`/lessons/admin/${id}/unpublish`)
}

export async function deleteLesson(id: string): Promise<void> {
  await apiClient.delete(`/lessons/admin/${id}`)
}

export interface LessonSenseRef {
  lessonId: string
  senseId: string
}

export async function attachLessonSense({ lessonId, senseId }: LessonSenseRef): Promise<void> {
  await apiClient.post(`/lessons/admin/${lessonId}/senses`, { senseId })
}

export async function detachLessonSense({ lessonId, senseId }: LessonSenseRef): Promise<void> {
  await apiClient.delete(`/lessons/admin/${lessonId}/senses/${senseId}`)
}

export interface ReorderLessonSensesPayload {
  lessonId: string
  senseIds: string[]
}

export async function reorderLessonSenses({ lessonId, senseIds }: ReorderLessonSensesPayload): Promise<void> {
  await apiClient.put(`/lessons/admin/${lessonId}/senses/order`, { senseIds })
}
