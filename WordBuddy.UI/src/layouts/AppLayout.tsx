import type { ReactElement } from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'

/** A sidebar navigation entry. */
interface NavItem {
  to: string
  label: string
  icon: string
  end: boolean
  /**
   * Exact paths this item owns. When set, the item is active only on these paths
   * (instead of NavLink's own prefix/exact matching on `to`).
   */
  activePaths?: string[]
}

const NAV_ITEMS: NavItem[] = [
  { to: '/', label: 'Home', icon: '🏠', end: true },
  { to: '/lessons', label: 'Lessons', icon: '📚', end: false },
  { to: '/vocabulary', label: 'My Vocabulary', icon: '📝', end: false },
  { to: '/vocabulary/shared', label: 'Shared Pool', icon: '🌍', end: false },
  { to: '/progress', label: 'My Progress', icon: '✅', end: false },
]

const ADMIN_NAV_ITEM: NavItem = { to: '/vocabulary/moderation', label: 'Moderation', icon: '🛡️', end: false }

/** Sidebar + top bar shell for all protected pages — large icons and labels, child-friendly. */
export function AppLayout(): ReactElement {
  const { user, logout } = useAuthStore()
  const navItems: NavItem[] = user?.isAdmin ? [...NAV_ITEMS, ADMIN_NAV_ITEM] : NAV_ITEMS

  return (
    <div className="flex min-h-screen bg-wb-surface-page">
      <aside className="flex w-64 flex-shrink-0 flex-col gap-2 bg-wb-surface-card p-4 shadow-wb-raised">
        <div className="mb-4 flex items-center gap-2 px-2">
          <span className="text-3xl">📚</span>
          <span className="text-xl font-extrabold text-wb-ink-muted">WordBuddy</span>
        </div>

        {navItems.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.end}
            className={({ isActive }) =>
              `flex items-center gap-3 rounded-wb-lg px-4 py-3 text-lg font-semibold ${
                isActive
                  ? 'bg-wb-primary text-wb-on-primary shadow-wb-card'
                  : 'text-wb-ink hover:bg-wb-hover-tint'
              }`
            }
          >
            <span className="text-2xl">{item.icon}</span>
            {item.label}
          </NavLink>
        ))}

        <div className="mt-auto flex items-center gap-3 rounded-wb-lg px-4 py-3">
          <span className="text-2xl">👤</span>
          <div className="flex-1">
            <p className="text-sm font-semibold text-wb-ink">{user?.displayName}</p>
          </div>
        </div>
        <button
          type="button"
          onClick={logout}
          className="rounded-wb-lg px-4 py-3 text-left text-lg font-semibold text-wb-danger hover:bg-wb-danger-soft"
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
