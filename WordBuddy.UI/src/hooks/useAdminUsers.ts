import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { getAdminUsers, setModeratorRole } from '../api/adminUsers'
import type { PageParams } from '../types'

/** One page of adult accounts. Keeps the previous page on screen while the next one loads. */
export function useAdminUsers(search: string, paging: PageParams, enabled: boolean) {
  return useQuery({
    queryKey: ['admin-users', search, paging.page, paging.pageSize],
    queryFn: () => getAdminUsers({ search: search === '' ? undefined : search, ...paging }),
    enabled,
    placeholderData: keepPreviousData,
  })
}

/** Grants or revokes Moderator, then refetches the user list. */
export function useSetModeratorRole() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: setModeratorRole,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-users'] }),
  })
}
