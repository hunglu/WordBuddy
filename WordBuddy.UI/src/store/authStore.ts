import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { AuthToken, User } from '../types'

interface AuthState {
  token: string | null
  expiresAtUtc: string | null
  user: User | null
  isAuthenticated: boolean
  login: (auth: AuthToken) => void
  logout: () => void
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      token: null,
      expiresAtUtc: null,
      user: null,
      isAuthenticated: false,
      login: (auth) =>
        set({
          token: auth.token,
          expiresAtUtc: auth.expiresAtUtc,
          user: auth.user,
          isAuthenticated: true,
        }),
      logout: () =>
        set({ token: null, expiresAtUtc: null, user: null, isAuthenticated: false }),
    }),
    { name: 'wordbuddy-auth' },
  ),
)
