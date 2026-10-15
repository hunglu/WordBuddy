import { keepPreviousData, useMutation, useQuery, useQueryClient, type QueryClient } from '@tanstack/react-query'
import {
  attachLessonSense,
  createLesson,
  deleteLesson,
  detachLessonSense,
  getAdminLessonDetail,
  getAdminLessons,
  publishLesson,
  reorderLessonSenses,
  searchLessonSenses,
  unpublishLesson,
  updateLesson,
} from '../api/lessons'
import type { AdminLessonFilters, PageParams } from '../types'

/** Refetches every admin and learner lesson view after a write. */
function invalidateLessons(queryClient: QueryClient): Promise<unknown[]> {
  return Promise.all([
    queryClient.invalidateQueries({ queryKey: ['adminLessons'] }),
    queryClient.invalidateQueries({ queryKey: ['adminLesson'] }),
    queryClient.invalidateQueries({ queryKey: ['lessons'] }),
    queryClient.invalidateQueries({ queryKey: ['lesson'] }),
  ])
}

/** One page of lessons. Keeps the previous page on screen while the next one loads. */
export function useAdminLessons(filters: AdminLessonFilters, paging: PageParams, enabled: boolean) {
  return useQuery({
    queryKey: ['adminLessons', filters, paging],
    queryFn: () => getAdminLessons(filters, paging),
    enabled,
    placeholderData: keepPreviousData,
  })
}

export function useAdminLesson(id: string | undefined, enabled: boolean) {
  return useQuery({
    queryKey: ['adminLesson', id],
    queryFn: () => getAdminLessonDetail(id as string),
    enabled: enabled && id !== undefined,
  })
}

export function useLessonSenseSearch(search: string, enabled: boolean) {
  return useQuery({
    queryKey: ['adminLessonSenses', search],
    queryFn: () => searchLessonSenses(search),
    enabled,
  })
}

export function useCreateLesson() {
  const queryClient = useQueryClient()
  return useMutation({ mutationFn: createLesson, onSuccess: () => invalidateLessons(queryClient) })
}

export function useUpdateLesson() {
  const queryClient = useQueryClient()
  return useMutation({ mutationFn: updateLesson, onSuccess: () => invalidateLessons(queryClient) })
}

export function useSetLessonPublished() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, publish }: { id: string; publish: boolean }) => (publish ? publishLesson(id) : unpublishLesson(id)),
    onSuccess: () => invalidateLessons(queryClient),
  })
}

export function useDeleteLesson() {
  const queryClient = useQueryClient()
  return useMutation({ mutationFn: deleteLesson, onSuccess: () => invalidateLessons(queryClient) })
}

export function useAttachLessonSense() {
  const queryClient = useQueryClient()
  return useMutation({ mutationFn: attachLessonSense, onSuccess: () => invalidateLessons(queryClient) })
}

export function useDetachLessonSense() {
  const queryClient = useQueryClient()
  return useMutation({ mutationFn: detachLessonSense, onSuccess: () => invalidateLessons(queryClient) })
}

export function useReorderLessonSenses() {
  const queryClient = useQueryClient()
  return useMutation({ mutationFn: reorderLessonSenses, onSuccess: () => invalidateLessons(queryClient) })
}
