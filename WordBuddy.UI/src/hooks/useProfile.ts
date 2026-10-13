import { useMutation, useQueryClient } from '@tanstack/react-query'
import { changePassword, updateDateOfBirth, updateDisplayName } from '../api/auth'
import { useAuthStore } from '../store/authStore'
import { CURRENT_USER_KEY, SUPPORT_LINKS_KEY } from './useSupportLinks'

/** Saves the date of birth. The reply carries a fresh token (new `age_band` claim), which replaces the session. */
export function useUpdateDateOfBirth() {
  const queryClient = useQueryClient()
  const login = useAuthStore((state) => state.login)
  return useMutation({
    mutationFn: updateDateOfBirth,
    onSuccess: (auth) => {
      login(auth)
      queryClient.setQueryData(CURRENT_USER_KEY, auth.user)
      void queryClient.invalidateQueries({ queryKey: CURRENT_USER_KEY })
      void queryClient.invalidateQueries({ queryKey: SUPPORT_LINKS_KEY })
    },
  })
}

/** Saves the display name and refreshes the cached profile. */
export function useUpdateDisplayName() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: updateDisplayName,
    onSuccess: (user) => {
      queryClient.setQueryData(CURRENT_USER_KEY, user)
      void queryClient.invalidateQueries({ queryKey: CURRENT_USER_KEY })
    },
  })
}

/** Changes the password. The reply carries a fresh token, which replaces the session. */
export function useChangePassword() {
  const queryClient = useQueryClient()
  const login = useAuthStore((state) => state.login)
  return useMutation({
    mutationFn: changePassword,
    onSuccess: (auth) => {
      login(auth)
      void queryClient.invalidateQueries({ queryKey: CURRENT_USER_KEY })
    },
  })
}
