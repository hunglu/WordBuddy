import type { ReactElement } from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'

const NAV_ITEMS = [
  { to: '/', label: 'Home', icon: '🏠', end: true },
  { to: '/lessons', label: 'Lessons', icon: '📚', end: false },
  { to: '/progress', label: 'My Progress', icon: '✅', end: false },
]

/** Sidebar + top bar shell for all protected pages — large icons and labels, child-friendly. */
export function AppLayout(): ReactElement {
  const { user, logout } = useAuthStore()

  return (
    <div className="flex min-h-screen bg-sky-50">
      <aside className="flex w-64 flex-shrink-0 flex-col gap-2 bg-white p-4 shadow-md">
        <div className="mb-4 flex items-center gap-2 px-2">
          <span className="text-3xl">📚</span>
          <span className="text-xl font-extrabold text-sky-700">WordBuddy</span>
        </div>

        {NAV_ITEMS.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.end}
            className={({ isActive }) =>
              `flex items-center gap-3 rounded-2xl px-4 py-3 text-lg font-semibold transition-colors ${
                isActive
                  ? 'bg-sky-500 text-white shadow'
                  : 'text-sky-900 hover:bg-sky-100'
              }`
            }
          >
            <span className="text-2xl">{item.icon}</span>
            {item.label}
          </NavLink>
        ))}

        <div className="mt-auto flex items-center gap-3 rounded-2xl px-4 py-3">
          <span className="text-2xl">👤</span>
          <div className="flex-1">
            <p className="text-sm font-semibold text-sky-900">{user?.displayName}</p>
          </div>
        </div>
        <button
          type="button"
          onClick={logout}
          className="rounded-2xl px-4 py-3 text-left text-lg font-semibold text-rose-600 hover:bg-rose-50"
        >
          Log out
        </button>
      </aside>

      <main className="flex-1 p-6">
        <Outlet />
      </main>
    </div>
  )
}
