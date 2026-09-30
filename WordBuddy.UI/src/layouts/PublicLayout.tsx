import type { ReactElement } from 'react'
import { Outlet } from 'react-router-dom'

/** Centered card layout for /login and /register. */
export function PublicLayout(): ReactElement {
  return (
    <div className="flex min-h-screen items-center justify-center bg-gradient-to-br from-wb-auth-from via-wb-auth-via to-wb-auth-to px-4">
      <div className="w-full max-w-md rounded-wb-card bg-wb-surface-card p-8 shadow-wb-overlay">
        <div className="mb-6 text-center">
          <span className="text-4xl">📚</span>
          <h1 className="mt-2 text-2xl font-extrabold text-wb-ink-muted">WordBuddy</h1>
        </div>
        <Outlet />
      </div>
    </div>
  )
}
