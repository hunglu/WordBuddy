import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Navigate } from 'react-router-dom'
import { problemMessage } from '../api/supportLinks'
import { Pager } from '../components/Pager'
import { cardClass, dangerButtonClass, inputClass, primaryButtonClass } from '../components/support/supportUi'
import { useAdminUsers, useSetModeratorRole } from '../hooks/useAdminUsers'
import { usePagination } from '../hooks/usePagination'
import { useAuthStore } from '../store/authStore'
import { isAdmin } from '../utils/roles'

const SEARCH_DEBOUNCE_MS = 300
const SEARCH_MAX = 100

const badgeClass = 'rounded-full bg-wb-hover-tint px-2 py-0.5 text-xs font-semibold text-wb-ink'

/** `/admin/users` — list adult accounts and grant or revoke the Moderator role. Admin only. */
export function AdminUsersPage(): ReactElement {
  const { t } = useTranslation('admin')
  const admin: boolean = isAdmin(useAuthStore((state) => state.user))

  const [searchText, setSearchText] = useState('')
  const [search, setSearch] = useState('')
  const pagination = usePagination()
  const resetPage = pagination.reset

  // Wait for a pause in typing before asking the server; a new search starts at page 1.
  useEffect(() => {
    const timer = setTimeout(() => {
      setSearch(searchText.trim())
      resetPage()
    }, SEARCH_DEBOUNCE_MS)
    return () => clearTimeout(timer)
  }, [searchText, resetPage])

  const users = useAdminUsers(search, pagination.params, admin)
  pagination.clamp(users.data?.totalCount)
  const setModerator = useSetModeratorRole()

  // Server-side, `AdminOnly` already rejects a non-admin call — this is a UX guard.
  if (!admin) {
    return <Navigate to="/" replace />
  }

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <h1 className="text-3xl font-extrabold text-wb-ink">{t('users.title')}</h1>
      <p className="mt-2 text-wb-ink-muted">{t('users.hint')}</p>

      <label htmlFor="userSearch" className="mt-6 block text-sm font-semibold text-wb-ink">
        {t('users.search')}
      </label>
      <input
        id="userSearch"
        value={searchText}
        maxLength={SEARCH_MAX}
        onChange={(e) => setSearchText(e.target.value)}
        className={inputClass}
      />

      {users.isLoading && <p className="mt-4 text-wb-ink-muted">{t('users.loading')}</p>}
      {users.isError && <p className="mt-4 text-wb-danger">{t('users.loadFailed')}</p>}
      {users.data && users.data.totalCount === 0 && <p className="mt-4 text-wb-ink-muted">{t('users.none')}</p>}

      <div className="mt-4 flex flex-col gap-3">
        {users.data?.items.map((user) => (
          <div key={user.id} className={`${cardClass} flex items-center justify-between gap-4`}>
            <div>
              <p className="font-semibold text-wb-ink">{user.displayName}</p>
              <p className="text-sm text-wb-ink-muted">{user.email}</p>
              <div className="mt-2 flex gap-2">
                {user.isAdmin && <span className={badgeClass}>{t('users.admin')}</span>}
                {user.isModerator && <span className={badgeClass}>{t('users.moderator')}</span>}
                {user.isGuardian && <span className={badgeClass}>{t('users.guardian')}</span>}
              </div>
            </div>
            {!user.isAdmin && (
              <button
                type="button"
                disabled={setModerator.isPending}
                onClick={() => setModerator.mutate({ id: user.id, isModerator: !user.isModerator })}
                className={user.isModerator ? dangerButtonClass : primaryButtonClass}
              >
                {user.isModerator ? t('users.revoke') : t('users.grant')}
              </button>
            )}
          </div>
        ))}
      </div>

      {users.data && (
        <Pager
          page={pagination.page}
          pageSize={pagination.pageSize}
          totalCount={users.data.totalCount}
          onPageChange={pagination.setPage}
          label={t('users.pages')}
          itemLabel={t('users.items')}
        />
      )}
      {setModerator.isError && (
        <p className="mt-2 text-sm text-wb-danger">{problemMessage(setModerator.error, t('users.failed'))}</p>
      )}
    </motion.div>
  )
}
