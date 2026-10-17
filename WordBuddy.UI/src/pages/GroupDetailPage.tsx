import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { problemMessage } from '../api/supportLinks'
import { GroupDashboardTab } from '../components/groups/GroupDashboardTab'
import { GroupMembersTab } from '../components/groups/GroupMembersTab'
import { GroupWordsTab } from '../components/groups/GroupWordsTab'
import { inlineErrorClass, isForbidden } from '../components/groups/groupUi'
import { dangerButtonClass, inputClass, primaryButtonClass, softButtonClass } from '../components/support/supportUi'
import { useDeleteGroup, useGroup, useRenameGroup } from '../hooks/useGroups'

type GroupTab = 'members' | 'words' | 'dashboard'

const TABS: readonly GroupTab[] = ['members', 'words', 'dashboard']

/** `/groups/:groupId` — the owner's view of one group: members, words and dashboard. */
export function GroupDetailPage(): ReactElement {
  const { t } = useTranslation('groups')
  const { groupId = '' } = useParams<{ groupId: string }>()
  const navigate = useNavigate()
  const { data: group, isLoading, isError, error } = useGroup(groupId)
  const renameGroup = useRenameGroup()
  const deleteGroup = useDeleteGroup()
  const [tab, setTab] = useState<GroupTab>('members')
  const [newName, setNewName] = useState<string | null>(null)
  const [confirmDelete, setConfirmDelete] = useState<boolean>(false)

  function submitRename(): void {
    if (newName === null) {
      return
    }
    renameGroup.mutate({ groupId, name: newName }, { onSuccess: () => setNewName(null) })
  }

  function submitDelete(): void {
    deleteGroup.mutate(groupId, { onSuccess: () => navigate('/groups') })
  }

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <Link to="/groups" className="text-sm text-wb-primary underline">
        {t('detail.all')}
      </Link>

      {isLoading && <p className="mt-6 text-lg text-wb-ink-muted">{t('detail.loading')}</p>}

      {isError && (
        <p className={`mt-6 text-lg ${inlineErrorClass}`}>
          {isForbidden(error) ? t('noAccess') : t('detail.loadFailed')}
        </p>
      )}

      {group && (
        <>
          <div className="mt-2 flex flex-wrap items-center justify-between gap-3">
            {newName === null ? (
              <h1 className="text-3xl font-extrabold text-wb-ink">{group.name}</h1>
            ) : (
              <div className="flex flex-wrap items-center gap-2">
                <label htmlFor="rename-group" className="sr-only">
                  {t('detail.nameLabel')}
                </label>
                <input
                  id="rename-group"
                  type="text"
                  value={newName}
                  onChange={(event) => setNewName(event.target.value)}
                  className={inputClass}
                />
                <button type="button" disabled={renameGroup.isPending} onClick={submitRename} className={primaryButtonClass}>
                  {t('save', { ns: 'common' })}
                </button>
                <button type="button" onClick={() => setNewName(null)} className={softButtonClass}>
                  {t('cancel', { ns: 'common' })}
                </button>
              </div>
            )}
            <div className="flex gap-2">
              {newName === null && (
                <button type="button" onClick={() => setNewName(group.name)} className={softButtonClass}>
                  {t('detail.rename')}
                </button>
              )}
              {!confirmDelete ? (
                <button type="button" onClick={() => setConfirmDelete(true)} className={dangerButtonClass}>
                  {t('delete', { ns: 'common' })}
                </button>
              ) : (
                <>
                  <button type="button" disabled={deleteGroup.isPending} onClick={submitDelete} className={dangerButtonClass}>
                    {t('detail.deleteGroup')}
                  </button>
                  <button type="button" onClick={() => setConfirmDelete(false)} className={softButtonClass}>
                    {t('detail.keep')}
                  </button>
                </>
              )}
            </div>
          </div>
          {renameGroup.isError && (
            <p className={`mt-2 ${inlineErrorClass}`}>{problemMessage(renameGroup.error, t('detail.renameFailed'))}</p>
          )}
          {deleteGroup.isError && (
            <p className={`mt-2 ${inlineErrorClass}`}>{problemMessage(deleteGroup.error, t('detail.deleteFailed'))}</p>
          )}

          <div className="mt-6 flex gap-2" role="tablist" aria-label={t('detail.sections')}>
            {TABS.map((item) => (
              <button
                key={item}
                type="button"
                role="tab"
                aria-selected={tab === item}
                onClick={() => setTab(item)}
                className={tab === item ? primaryButtonClass : softButtonClass}
              >
                {t(`detail.tabs.${item}`)}
              </button>
            ))}
          </div>

          <div className="mt-4" role="tabpanel">
            {tab === 'members' && <GroupMembersTab group={group} />}
            {tab === 'words' && <GroupWordsTab group={group} />}
            {tab === 'dashboard' && <GroupDashboardTab group={group} />}
          </div>
        </>
      )}
    </motion.div>
  )
}
