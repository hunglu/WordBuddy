import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { problemMessage } from '../api/supportLinks'
import { GroupDashboardTab } from '../components/groups/GroupDashboardTab'
import { GroupMembersTab } from '../components/groups/GroupMembersTab'
import { GroupWordsTab } from '../components/groups/GroupWordsTab'
import { NO_ACCESS_MESSAGE, inlineErrorClass, isForbidden } from '../components/groups/groupUi'
import { dangerButtonClass, inputClass, primaryButtonClass, softButtonClass } from '../components/support/supportUi'
import { useDeleteGroup, useGroup, useRenameGroup } from '../hooks/useGroups'

type GroupTab = 'members' | 'words' | 'dashboard'

const TABS: readonly { id: GroupTab; label: string }[] = [
  { id: 'members', label: 'Members' },
  { id: 'words', label: 'Words' },
  { id: 'dashboard', label: 'Dashboard' },
]

/** `/groups/:groupId` — the owner's view of one group: members, words and dashboard. */
export function GroupDetailPage(): ReactElement {
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
        All groups
      </Link>

      {isLoading && <p className="mt-6 text-lg text-wb-ink-muted">Loading the group…</p>}

      {isError && (
        <p className={`mt-6 text-lg ${inlineErrorClass}`}>
          {isForbidden(error) ? NO_ACCESS_MESSAGE : "Couldn't load the group right now. Please try again later."}
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
                  Group name
                </label>
                <input
                  id="rename-group"
                  type="text"
                  value={newName}
                  onChange={(event) => setNewName(event.target.value)}
                  className={inputClass}
                />
                <button type="button" disabled={renameGroup.isPending} onClick={submitRename} className={primaryButtonClass}>
                  Save
                </button>
                <button type="button" onClick={() => setNewName(null)} className={softButtonClass}>
                  Cancel
                </button>
              </div>
            )}
            <div className="flex gap-2">
              {newName === null && (
                <button type="button" onClick={() => setNewName(group.name)} className={softButtonClass}>
                  Rename
                </button>
              )}
              {!confirmDelete ? (
                <button type="button" onClick={() => setConfirmDelete(true)} className={dangerButtonClass}>
                  Delete
                </button>
              ) : (
                <>
                  <button type="button" disabled={deleteGroup.isPending} onClick={submitDelete} className={dangerButtonClass}>
                    Delete group and remove all members
                  </button>
                  <button type="button" onClick={() => setConfirmDelete(false)} className={softButtonClass}>
                    Keep
                  </button>
                </>
              )}
            </div>
          </div>
          {renameGroup.isError && (
            <p className={`mt-2 ${inlineErrorClass}`}>{problemMessage(renameGroup.error, 'Could not rename the group.')}</p>
          )}
          {deleteGroup.isError && (
            <p className={`mt-2 ${inlineErrorClass}`}>{problemMessage(deleteGroup.error, 'Could not delete the group.')}</p>
          )}

          <div className="mt-6 flex gap-2" role="tablist" aria-label="Group sections">
            {TABS.map((item) => (
              <button
                key={item.id}
                type="button"
                role="tab"
                aria-selected={tab === item.id}
                onClick={() => setTab(item.id)}
                className={tab === item.id ? primaryButtonClass : softButtonClass}
              >
                {item.label}
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
