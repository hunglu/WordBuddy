import { zodResolver } from '@hookform/resolvers/zod'
import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate } from 'react-router-dom'
import { z } from 'zod'
import { problemMessage } from '../api/supportLinks'
import { inlineErrorClass } from '../components/groups/groupUi'
import { avatarEmoji, cardClass, inputClass, primaryButtonClass, softButtonClass } from '../components/support/supportUi'
import { useCreateGroup, useLeaveGroup, useMyGroups } from '../hooks/useGroups'
import { useAuthStore } from '../store/authStore'

const createGroupSchema = z.object({
  name: z.string().trim().min(3, 'Use at least 3 characters').max(60, 'Use at most 60 characters'),
})

type CreateGroupFormValues = z.infer<typeof createGroupSchema>

/**
 * `/groups` — a supporter's groups with a create form. Every learner also sees the groups they joined
 * (group name and owner only; never other members). A child cannot create groups or leave one.
 */
export function GroupsPage(): ReactElement {
  const navigate = useNavigate()
  const isChild = useAuthStore((state) => state.user?.ageGroup === 'Child')
  const { data, isLoading, isError, error } = useMyGroups()
  const createGroup = useCreateGroup()
  const leaveGroup = useLeaveGroup()

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<CreateGroupFormValues>({ resolver: zodResolver(createGroupSchema) })

  function onCreate(values: CreateGroupFormValues): void {
    createGroup.mutate(values.name, {
      onSuccess: (groupId) => {
        reset()
        navigate(`/groups/${groupId}`)
      },
    })
  }

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <h1 className="text-3xl font-extrabold text-wb-ink">Groups</h1>
      <p className="mt-2 text-wb-ink-muted">
        {isChild
          ? 'Groups your supporter added you to.'
          : 'Put learners you support into a group, give them words together, and see how the group is doing.'}
      </p>

      {isLoading && <p className="mt-8 text-lg text-wb-ink-muted">Loading your groups…</p>}
      {isError && (
        <p className={`mt-8 text-lg ${inlineErrorClass}`}>{problemMessage(error, "Couldn't load your groups right now.")}</p>
      )}

      {!isChild && (
        <form onSubmit={handleSubmit(onCreate)} className={`${cardClass} mt-6 flex flex-col gap-3`} aria-label="Create a group">
          <label htmlFor="group-name" className="text-lg font-bold text-wb-ink">
            New group
          </label>
          <input id="group-name" type="text" autoComplete="off" className={inputClass} {...register('name')} />
          {errors.name && <p className={inlineErrorClass}>{errors.name.message}</p>}
          {createGroup.isError && (
            <p className={inlineErrorClass}>{problemMessage(createGroup.error, 'Could not create the group.')}</p>
          )}
          <button type="submit" disabled={createGroup.isPending} className={`${primaryButtonClass} self-start`}>
            Create group
          </button>
        </form>
      )}

      {data && !isChild && (
        <section className="mt-8" aria-label="My groups">
          <h2 className="text-2xl font-bold text-wb-ink">My groups</h2>
          {data.owned.length === 0 ? (
            <p className="mt-2 text-wb-ink-muted">You have no groups yet.</p>
          ) : (
            <div className="mt-3 flex flex-col gap-3">
              {data.owned.map((group) => (
                <Link key={group.id} to={`/groups/${group.id}`} className={`${cardClass} block hover:bg-wb-hover-tint`}>
                  <p className="text-xl font-bold text-wb-ink">{group.name}</p>
                  <p className="text-sm text-wb-ink-muted">
                    {group.activeMemberCount} active
                    {group.pendingMemberCount > 0 ? ` · ${group.pendingMemberCount} waiting for approval` : ''}
                  </p>
                </Link>
              ))}
            </div>
          )}
        </section>
      )}

      {data && (
        <section className="mt-8" aria-label="Groups I joined">
          <h2 className="text-2xl font-bold text-wb-ink">Groups I joined</h2>
          {data.joined.length === 0 ? (
            <p className="mt-2 text-wb-ink-muted">You are not in a group.</p>
          ) : (
            <div className="mt-3 flex flex-col gap-3">
              {data.joined.map((group) => (
                <div key={group.id} className={`${cardClass} flex flex-wrap items-center justify-between gap-3`}>
                  <div>
                    <p className="text-xl font-bold text-wb-ink">{group.name}</p>
                    <p className="text-sm text-wb-ink-muted">
                      <span aria-hidden="true">{avatarEmoji(group.ownerAvatarId)} </span>
                      {group.ownerName}
                    </p>
                  </div>
                  {!isChild && (
                    <button
                      type="button"
                      disabled={leaveGroup.isPending}
                      onClick={() => leaveGroup.mutate(group.id)}
                      className={softButtonClass}
                    >
                      Leave
                    </button>
                  )}
                </div>
              ))}
            </div>
          )}
          {leaveGroup.isError && (
            <p className={`mt-2 ${inlineErrorClass}`}>{problemMessage(leaveGroup.error, 'Could not leave the group.')}</p>
          )}
        </section>
      )}
    </motion.div>
  )
}
