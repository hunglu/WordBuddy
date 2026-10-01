import type { ReactElement } from 'react'
import { Link, Outlet, matchPath, useLocation } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'

/** A sidebar navigation entry. */
interface NavItem {
  to: string
  label: string
  icon: string
  end: boolean
  /**
   * Exact paths this item owns. When set, the item is active only on these paths
   * (instead of the default prefix/exact matching on `to`).
   */
  activePaths?: string[]
}

const NAV_ITEMS: NavItem[] = [
  { to: '/', label: 'Home', icon: '🏠', end: true },
  { to: '/lessons', label: 'Lessons', icon: '📚', end: false },
  {
    to: '/vocabulary',
    label: 'My Vocabulary',
    icon: '📝',
    end: true,
    activePaths: ['/vocabulary', '/vocabulary/check'],
  },
  { to: '/vocabulary/shared', label: 'Shared Pool', icon: '🌍', end: true },
  { to: '/progress', label: 'My Progress', icon: '✅', end: false },
]

const ADMIN_NAV_ITEM: NavItem = { to: '/vocabulary/moderation', label: 'Moderation', icon: '🛡️', end: true }

/**
 * Whether a nav item is the selected one for `pathname`: exact match on `activePaths` when set,
 * otherwise an exact (`end`) or prefix match on `to` (driven by `end`).
 */
function isItemActive(item: NavItem, pathname: string): boolean {
  const paths: string[] = item.activePaths ?? [item.to]
  const end: boolean = item.activePaths ? true : item.end
  return paths.some((p) => matchPath({ path: p, end }, pathname) !== null)
}

/** Sidebar + top bar shell for all protected pages — large icons and labels, child-friendly. */
export function AppLayout(): ReactElement {
  const { user, logout } = useAuthStore()
  const navItems: NavItem[] = user?.isAdmin ? [...NAV_ITEMS, ADMIN_NAV_ITEM] : NAV_ITEMS
  const { pathname } = useLocation()

  return (
    <div className="flex min-h-screen bg-wb-surface-page">
      <aside className="flex w-64 flex-shrink-0 flex-col gap-2 bg-wb-surface-card p-4 shadow-wb-raised">
        <div className="mb-4 flex items-center gap-2 px-2">
          <span className="text-3xl">📚</span>
          <span className="text-xl font-extrabold text-wb-ink-muted">WordBuddy</span>
        </div>

        {navItems.map((item) => {
          const active: boolean = isItemActive(item, pathname)
          return (
            <Link
              key={item.to}
              to={item.to}
              aria-current={active ? 'page' : undefined}
              className={`flex items-center gap-3 rounded-wb-lg px-4 py-3 text-lg font-semibold ${
                active
                  ? 'bg-wb-primary text-wb-on-primary shadow-wb-card'
                  : 'text-wb-ink hover:bg-wb-hover-tint'
              }`}
            >
              <span className="text-2xl">{item.icon}</span>
              {item.label}
            </Link>
          )
        })}

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
