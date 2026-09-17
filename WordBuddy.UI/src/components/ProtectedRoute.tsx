import type { ReactElement } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'
import { isJwtExpired } from '../utils/jwt'

/** Redirects to /login when there's no token, or when the JWT's own `exp` claim has passed. */
export function ProtectedRoute({ children }: { children: ReactElement }): ReactElement {
  const { token, logout } = useAuthStore()

  if (!token || isJwtExpired(token)) {
    if (token) {
      logout()
    }
    return <Navigate to="/login" replace />
  }

  return children
}
