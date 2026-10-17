import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, Navigate } from 'react-router-dom'
import { problemMessage } from '../api/supportLinks'
import { ConfirmDialog } from '../components/admin-lessons/ConfirmDialog'
import { LessonStatusBadge } from '../components/admin-lessons/LessonStatusBadge'
import { Pager } from '../components/Pager'
import { cardClass, dangerButtonClass, inputClass, primaryButtonClass, softButtonClass } from '../components/support/supportUi'
import { useAdminLessons, useDeleteLesson, useSetLessonPublished } from '../hooks/useAdminLessons'
import { usePagination } from '../hooks/usePagination'
import { useAuthStore } from '../store/authStore'
import { canManageContent } from '../utils/roles'
import { levelClasses } from '../theme/variants'
import type { AdminLessonDto, Level, LessonType } from '../types'

type StatusFilter = 'all' | 'published' | 'draft'

const LEVELS: Level[] = ['Beginner', 'Intermediate', 'Advanced']
const TYPES: LessonType[] = ['Vocabulary', 'Grammar', 'DailyPhrase']

/** `/admin/lessons` — every lesson, drafts included. Admin or moderator. */
export function AdminLessonsPage(): ReactElement {
  const { t } = useTranslation(['admin', 'lessons', 'common'])
  const user = useAuthStore((state) => state.user)
  const canManage: boolean = canManageContent(user)

  const [type, setType] = useState<LessonType | ''>('')
  const [level, setLevel] = useState<Level | ''>('')
  const [status, setStatus] = useState<StatusFilter>('all')
  const [toDelete, setToDelete] = useState<AdminLessonDto | null>(null)
  const pagination = usePagination()

  const lessons = useAdminLessons(
    {
      type: type === '' ? undefined : type,
      level: level === '' ? undefined : level,
      isPublished: status === 'all' ? undefined : status === 'published',
    },
    pagination.params,
    canManage,
  )
  pagination.clamp(lessons.data?.totalCount)
  const setPublished = useSetLessonPublished()
  const remove = useDeleteLesson()

  // Server-side, `ManageContent` already rejects a non-admin call — this is a UX guard.
  if (!canManage) {
    return <Navigate to="/" replace />
  }

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-3xl font-extrabold text-wb-ink">{t('admin:lessons.title')}</h1>
          <p className="mt-1 text-wb-ink-muted">{t('admin:lessons.intro')}</p>
        </div>
        <Link to="/admin/lessons/new" className={primaryButtonClass}>
          {t('admin:lessons.newLesson')}
        </Link>
      </div>

      <div className="mt-6 flex flex-wrap gap-3">
        <label className="text-sm font-semibold text-wb-ink">
          {t('admin:lessons.filterType')}
          <select value={type} onChange={(e) => {
              setType(e.target.value as LessonType | '')
              pagination.reset()
            }}
            className={`${inputClass} mt-1`}>
            <option value="">{t('admin:lessons.allTypes')}</option>
            {TYPES.map((lessonType) => (
              <option key={lessonType} value={lessonType}>
                {t(`admin:lessons.type.${lessonType}`)}
              </option>
            ))}
          </select>
        </label>
        <label className="text-sm font-semibold text-wb-ink">
          {t('admin:lessons.filterLevel')}
          <select value={level} onChange={(e) => {
              setLevel(e.target.value as Level | '')
              pagination.reset()
            }}
            className={`${inputClass} mt-1`}>
            <option value="">{t('admin:lessons.allLevels')}</option>
            {LEVELS.map((l) => (
              <option key={l} value={l}>
                {t(`lessons:level.${l}`)}
              </option>
            ))}
          </select>
        </label>
        <label className="text-sm font-semibold text-wb-ink">
          {t('admin:lessons.filterStatus')}
          <select value={status} onChange={(e) => {
              setStatus(e.target.value as StatusFilter)
              pagination.reset()
            }}
            className={`${inputClass} mt-1`}>
            <option value="all">{t('admin:lessons.all')}</option>
            <option value="published">{t('admin:lessons.published')}</option>
            <option value="draft">{t('admin:lessons.draft')}</option>
          </select>
        </label>
      </div>

      {lessons.isLoading && <p className="mt-8 text-lg text-wb-ink-muted">{t('admin:lessons.loading')}</p>}
      {lessons.isError && <p className="mt-8 text-lg text-wb-danger">{t('admin:lessons.loadFailed')}</p>}
      {lessons.data && lessons.data.items.length === 0 && <p className="mt-8 text-lg text-wb-ink-muted">{t('admin:lessons.none')}</p>}
      {setPublished.isError && (
        <p role="alert" className="mt-4 text-sm font-semibold text-wb-danger">
          {problemMessage(setPublished.error, t('admin:lessons.statusFailed'))}
        </p>
      )}

      {lessons.data && lessons.data.items.length > 0 && (
        <div className={`mt-6 overflow-x-auto ${cardClass}`}>
          <table className="w-full text-left">
            <thead>
              <tr className="text-sm text-wb-ink-muted">
                <th className="pb-2 pr-4 font-semibold">{t('admin:lessons.colTitle')}</th>
                <th className="pb-2 pr-4 font-semibold">{t('admin:lessons.colType')}</th>
                <th className="pb-2 pr-4 font-semibold">{t('admin:lessons.colLevel')}</th>
                <th className="pb-2 pr-4 font-semibold">{t('admin:lessons.colFor')}</th>
                <th className="pb-2 pr-4 font-semibold">{t('admin:lessons.colWords')}</th>
                <th className="pb-2 pr-4 font-semibold">{t('admin:lessons.colStatus')}</th>
                <th className="pb-2 font-semibold">{t('admin:lessons.colActions')}</th>
              </tr>
            </thead>
            <tbody>
              {lessons.data.items.map((lesson) => (
                <tr key={lesson.id} className="border-t border-wb-border-subtle">
                  <td className="py-3 pr-4 font-bold text-wb-ink">
                    <Link to={`/admin/lessons/${lesson.id}`} className="hover:underline">
                      {lesson.title}
                    </Link>
                  </td>
                  <td className="py-3 pr-4 text-wb-ink">{t(`admin:lessons.type.${lesson.type}`)}</td>
                  <td className="py-3 pr-4">
                    <span className={`rounded-wb-pill px-3 py-1 text-xs font-bold ${levelClasses[lesson.level]}`}>{t(`lessons:level.${lesson.level}`)}</span>
                  </td>
                  <td className="py-3 pr-4 text-wb-ink">{lesson.targetAgeGroup === 'Child' ? t('admin:lessons.forChildren') : t('admin:lessons.forAdults')}</td>
                  <td className="py-3 pr-4 text-wb-ink">{lesson.type === 'Vocabulary' ? lesson.senseCount : '—'}</td>
                  <td className="py-3 pr-4">
                    <LessonStatusBadge isPublished={lesson.isPublished} />
                  </td>
                  <td className="py-3">
                    <div className="flex flex-wrap gap-2">
                      <Link to={`/admin/lessons/${lesson.id}`} className={softButtonClass}>
                        {t('admin:lessons.edit')}
                      </Link>
                      <button
                        type="button"
                        disabled={setPublished.isPending}
                        onClick={() => setPublished.mutate({ id: lesson.id, publish: !lesson.isPublished })}
                        className={softButtonClass}
                      >
                        {lesson.isPublished ? t('admin:lessons.unpublish') : t('admin:lessons.publish')}
                      </button>
                      <button type="button" onClick={() => setToDelete(lesson)} className={dangerButtonClass}>
                        {t('common:delete')}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {lessons.data && (
        <Pager
          page={pagination.page}
          pageSize={pagination.pageSize}
          totalCount={lessons.data.totalCount}
          onPageChange={pagination.setPage}
          label={t('admin:lessons.pages')}
          itemLabel={t('admin:lessons.items')}
        />
      )}

      <ConfirmDialog
        open={toDelete !== null}
        title={t('admin:lessons.deleteTitle', { title: toDelete?.title ?? '' })}
        message={t('admin:lessons.deleteMessage')}
        confirmLabel={t('common:delete')}
        pendingLabel={t('common:deleting')}
        isPending={remove.isPending}
        errorMessage={remove.isError ? problemMessage(remove.error, t('admin:lessons.deleteFailed')) : null}
        onCancel={() => {
          remove.reset()
          setToDelete(null)
        }}
        onConfirm={() => {
          if (toDelete) remove.mutate(toDelete.id, { onSuccess: () => setToDelete(null) })
        }}
      />
    </motion.div>
  )
}
