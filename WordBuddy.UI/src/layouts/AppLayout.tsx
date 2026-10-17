import type { ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, Outlet, matchPath, useLocation } from 'react-router-dom'
import { LanguageSwitcher } from '../components/LanguageSwitcher'
import type { nav } from '../i18n/locales/en/nav'
import { useAuthStore } from '../store/authStore'
import { canManageContent, isAdmin } from '../utils/roles'

type NavLabelKey = keyof typeof nav.items

/** A sidebar navigation entry. */
interface NavItem {
  to: string
  labelKey: NavLabelKey
  icon: string
  end: boolean
  /**
   * Exact paths this item owns. When set, the item is active only on these paths
   * (instead of the default prefix/exact matching on `to`).
   */
  activePaths?: string[]
}

const NAV_ITEMS: NavItem[] = [
  { to: '/', labelKey: 'home', icon: '🏠', end: true },
  { to: '/lessons', labelKey: 'lessons', icon: '📚', end: false },
  {
    to: '/vocabulary',
    labelKey: 'vocabulary',
    icon: '📝',
    end: true,
  },
  { to: '/vocabulary/review', labelKey: 'review', icon: '🧠', end: true },
  { to: '/challenges', labelKey: 'challenges', icon: '🏆', end: false },
  { to: '/vocabulary/shared', labelKey: 'shared', icon: '🌍', end: true },
  { to: '/progress', labelKey: 'progress', icon: '✅', end: false },
  { to: '/support', labelKey: 'supporters', icon: '🤝', end: false },
  { to: '/groups', labelKey: 'groups', icon: '👥', end: false },
]

/** Content work: admins and adult moderators. */
const CONTENT_NAV_ITEMS: NavItem[] = [
  { to: '/vocabulary/moderation', labelKey: 'moderation', icon: '🛡️', end: true },
  { to: '/admin/lessons', labelKey: 'lessonAuthoring', icon: '✏️', end: false },
  { to: '/admin/lexemes', labelKey: 'lexemes', icon: '📖', end: false },
  { to: '/admin/support-links', labelKey: 'supportLinks', icon: '🔗', end: true },
]

/** Admin only: user and role management. */
const ADMIN_ONLY_NAV_ITEMS: NavItem[] = [{ to: '/admin/users', labelKey: 'users', icon: '🔑', end: true }]

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
  const { t } = useTranslation('nav')
  const { user, logout } = useAuthStore()
  const navItems: NavItem[] = [
    ...NAV_ITEMS,
    ...(canManageContent(user) ? CONTENT_NAV_ITEMS : []),
    ...(isAdmin(user) ? ADMIN_ONLY_NAV_ITEMS : []),
  ]
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
              {t(`items.${item.labelKey}`)}
            </Link>
          )
        })}

        <Link to="/profile" className="mt-auto flex items-center gap-3 rounded-wb-lg px-4 py-3 hover:bg-wb-hover-tint">
          <span className="text-2xl">👤</span>
          <div className="flex-1">
            <p className="text-sm font-semibold text-wb-ink">{user?.displayName}</p>
            <p className="text-xs text-wb-ink-muted">{t('myProfile')}</p>
          </div>
        </Link>
        <LanguageSwitcher />
        <button
          type="button"
          onClick={logout}
          className="rounded-wb-lg px-4 py-3 text-left text-lg font-semibold text-wb-danger hover:bg-wb-danger-soft"
        >
          {t('logOut')}
        </button>
      </aside>

      <main className="flex-1 p-6">
        <Outlet />
      </main>
    </div>
  )
}
