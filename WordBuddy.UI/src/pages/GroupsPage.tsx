import { zodResolver } from '@hookform/resolvers/zod'
import { motion } from 'framer-motion'
import { useMemo, type ReactElement } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import type { TFunction } from 'i18next'
import { Link, useNavigate } from 'react-router-dom'
import { z } from 'zod'
import { problemMessage } from '../api/supportLinks'
import { inlineErrorClass } from '../components/groups/groupUi'
import { Pager } from '../components/Pager'
import { avatarEmoji, cardClass, inputClass, primaryButtonClass, softButtonClass } from '../components/support/supportUi'
import { useCreateGroup, useJoinedGroups, useLeaveGroup, useOwnedGroups } from '../hooks/useGroups'
import { usePagination } from '../hooks/usePagination'
import { useAuthStore } from '../store/authStore'

function makeCreateGroupSchema(t: TFunction<'groups'>) {
  return z.object({
    name: z.string().trim().min(3, t('create.nameMin')).max(60, t('create.nameMax')),
  })
}

type CreateGroupFormValues = z.infer<ReturnType<typeof makeCreateGroupSchema>>

/**
 * `/groups` — a supporter's groups with a create form. Every learner also sees the groups they joined
 * (group name and owner only; never other members). A child cannot create groups or leave one.
 */
export function GroupsPage(): ReactElement {
  const { t } = useTranslation('groups')
  const createGroupSchema = useMemo(() => makeCreateGroupSchema(t), [t])
  const navigate = useNavigate()
  const isChild = useAuthStore((state) => state.user?.ageGroup === 'Child')
  const ownedPaging = usePagination()
  const joinedPaging = usePagination()
  // A child owns no group, so the owned list is not asked for.
  const owned = useOwnedGroups(ownedPaging.params, !isChild)
  const joined = useJoinedGroups(joinedPaging.params)
  ownedPaging.clamp(owned.data?.totalCount)
  joinedPaging.clamp(joined.data?.totalCount)
  const isLoading: boolean = owned.isLoading || joined.isLoading
  const isError: boolean = owned.isError || joined.isError
  const error: unknown = owned.error ?? joined.error
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
      <h1 className="text-3xl font-extrabold text-wb-ink">{t('title')}</h1>
      <p className="mt-2 text-wb-ink-muted">
        {isChild
          ? t('introChild')
          : t('introAdult')}
      </p>

      {isLoading && <p className="mt-8 text-lg text-wb-ink-muted">{t('loading')}</p>}
      {isError && (
        <p className={`mt-8 text-lg ${inlineErrorClass}`}>{problemMessage(error, t('loadFailed'))}</p>
      )}

      {!isChild && (
        <form onSubmit={handleSubmit(onCreate)} className={`${cardClass} mt-6 flex flex-col gap-3`} aria-label={t('create.ariaLabel')}>
          <label htmlFor="group-name" className="text-lg font-bold text-wb-ink">
            {t('create.label')}
          </label>
          <input id="group-name" type="text" autoComplete="off" className={inputClass} {...register('name')} />
          {errors.name && <p className={inlineErrorClass}>{errors.name.message}</p>}
          {createGroup.isError && (
            <p className={inlineErrorClass}>{problemMessage(createGroup.error, t('create.failed'))}</p>
          )}
          <button type="submit" disabled={createGroup.isPending} className={`${primaryButtonClass} self-start`}>
            {t('create.submit')}
          </button>
        </form>
      )}

      {owned.data && !isChild && (
        <section className="mt-8" aria-label={t('mine.heading')}>
          <h2 className="text-2xl font-bold text-wb-ink">{t('mine.heading')}</h2>
          {owned.data.totalCount === 0 ? (
            <p className="mt-2 text-wb-ink-muted">{t('mine.none')}</p>
          ) : (
            <div className="mt-3 flex flex-col gap-3">
              {owned.data.items.map((group) => (
                <Link key={group.id} to={`/groups/${group.id}`} className={`${cardClass} block hover:bg-wb-hover-tint`}>
                  <p className="text-xl font-bold text-wb-ink">{group.name}</p>
                  <p className="text-sm text-wb-ink-muted">
                    {t('mine.active', { count: group.activeMemberCount })}
                    {group.pendingMemberCount > 0 ? t('mine.waiting', { count: group.pendingMemberCount }) : ''}
                  </p>
                </Link>
              ))}
            </div>
          )}
          <Pager
            page={ownedPaging.page}
            pageSize={ownedPaging.pageSize}
            totalCount={owned.data.totalCount}
            onPageChange={ownedPaging.setPage}
            label={t('mine.pages')}
            itemLabel={t('mine.items')}
          />
        </section>
      )}

      {joined.data && (
        <section className="mt-8" aria-label={t('joined.heading')}>
          <h2 className="text-2xl font-bold text-wb-ink">{t('joined.heading')}</h2>
          {joined.data.totalCount === 0 ? (
            <p className="mt-2 text-wb-ink-muted">{t('joined.none')}</p>
          ) : (
            <div className="mt-3 flex flex-col gap-3">
              {joined.data.items.map((group) => (
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
                      {t('joined.leave')}
                    </button>
                  )}
                </div>
              ))}
            </div>
          )}
          <Pager
            page={joinedPaging.page}
            pageSize={joinedPaging.pageSize}
            totalCount={joined.data.totalCount}
            onPageChange={joinedPaging.setPage}
            label={t('joined.pages')}
            itemLabel={t('joined.items')}
          />
          {leaveGroup.isError && (
            <p className={`mt-2 ${inlineErrorClass}`}>{problemMessage(leaveGroup.error, t('joined.leaveFailed'))}</p>
          )}
        </section>
      )}
    </motion.div>
  )
}
