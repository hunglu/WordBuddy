import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useState } from 'react'
import { Link, Navigate } from 'react-router-dom'
import { problemMessage } from '../api/supportLinks'
import { ConfirmDialog } from '../components/admin-lessons/ConfirmDialog'
import { LessonStatusBadge } from '../components/admin-lessons/LessonStatusBadge'
import { cardClass, dangerButtonClass, inputClass, primaryButtonClass, softButtonClass } from '../components/support/supportUi'
import { useAdminLessons, useDeleteLesson, useSetLessonPublished } from '../hooks/useAdminLessons'
import { useAuthStore } from '../store/authStore'
import { levelClasses } from '../theme/variants'
import type { AdminLessonDto, Level, LessonType } from '../types'

type StatusFilter = 'all' | 'published' | 'draft'

const TYPE_LABELS: Record<LessonType, string> = {
  Vocabulary: 'Vocabulary',
  Grammar: 'Grammar',
  DailyPhrase: 'Daily phrase',
}

const LEVELS: Level[] = ['Beginner', 'Intermediate', 'Advanced']
const TYPES: LessonType[] = ['Vocabulary', 'Grammar', 'DailyPhrase']

/** `/admin/lessons` — every lesson, drafts included. Admin only. */
export function AdminLessonsPage(): ReactElement {
  const user = useAuthStore((state) => state.user)
  const isAdmin = user?.isAdmin === true

  const [type, setType] = useState<LessonType | ''>('')
  const [level, setLevel] = useState<Level | ''>('')
  const [status, setStatus] = useState<StatusFilter>('all')
  const [toDelete, setToDelete] = useState<AdminLessonDto | null>(null)

  const lessons = useAdminLessons(
    {
      type: type === '' ? undefined : type,
      level: level === '' ? undefined : level,
      isPublished: status === 'all' ? undefined : status === 'published',
    },
    isAdmin,
  )
  const setPublished = useSetLessonPublished()
  const remove = useDeleteLesson()

  // Server-side, `AdminOnly` already rejects a non-admin call — this is a UX guard.
  if (!isAdmin) {
    return <Navigate to="/" replace />
  }

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-3xl font-extrabold text-wb-ink">Lesson authoring</h1>
          <p className="mt-1 text-wb-ink-muted">Create lessons, add words, and publish when ready. Drafts stay hidden from learners.</p>
        </div>
        <Link to="/admin/lessons/new" className={primaryButtonClass}>
          New lesson
        </Link>
      </div>

      <div className="mt-6 flex flex-wrap gap-3">
        <label className="text-sm font-semibold text-wb-ink">
          Type
          <select value={type} onChange={(e) => setType(e.target.value as LessonType | '')} className={`${inputClass} mt-1`}>
            <option value="">All types</option>
            {TYPES.map((t) => (
              <option key={t} value={t}>
                {TYPE_LABELS[t]}
              </option>
            ))}
          </select>
        </label>
        <label className="text-sm font-semibold text-wb-ink">
          Level
          <select value={level} onChange={(e) => setLevel(e.target.value as Level | '')} className={`${inputClass} mt-1`}>
            <option value="">All levels</option>
            {LEVELS.map((l) => (
              <option key={l} value={l}>
                {l}
              </option>
            ))}
          </select>
        </label>
        <label className="text-sm font-semibold text-wb-ink">
          Status
          <select value={status} onChange={(e) => setStatus(e.target.value as StatusFilter)} className={`${inputClass} mt-1`}>
            <option value="all">All</option>
            <option value="published">Published</option>
            <option value="draft">Draft</option>
          </select>
        </label>
      </div>

      {lessons.isLoading && <p className="mt-8 text-lg text-wb-ink-muted">Loading lessons…</p>}
      {lessons.isError && <p className="mt-8 text-lg text-wb-danger">Couldn't load lessons right now.</p>}
      {lessons.data && lessons.data.length === 0 && <p className="mt-8 text-lg text-wb-ink-muted">No lessons match these filters.</p>}
      {setPublished.isError && (
        <p role="alert" className="mt-4 text-sm font-semibold text-wb-danger">
          {problemMessage(setPublished.error, 'Could not change the status.')}
        </p>
      )}

      {lessons.data && lessons.data.length > 0 && (
        <div className={`mt-6 overflow-x-auto ${cardClass}`}>
          <table className="w-full text-left">
            <thead>
              <tr className="text-sm text-wb-ink-muted">
                <th className="pb-2 pr-4 font-semibold">Title</th>
                <th className="pb-2 pr-4 font-semibold">Type</th>
                <th className="pb-2 pr-4 font-semibold">Level</th>
                <th className="pb-2 pr-4 font-semibold">For</th>
                <th className="pb-2 pr-4 font-semibold">Words</th>
                <th className="pb-2 pr-4 font-semibold">Status</th>
                <th className="pb-2 font-semibold">Actions</th>
              </tr>
            </thead>
            <tbody>
              {lessons.data.map((lesson) => (
                <tr key={lesson.id} className="border-t border-wb-border-subtle">
                  <td className="py-3 pr-4 font-bold text-wb-ink">
                    <Link to={`/admin/lessons/${lesson.id}`} className="hover:underline">
                      {lesson.title}
                    </Link>
                  </td>
                  <td className="py-3 pr-4 text-wb-ink">{TYPE_LABELS[lesson.type]}</td>
                  <td className="py-3 pr-4">
                    <span className={`rounded-wb-pill px-3 py-1 text-xs font-bold ${levelClasses[lesson.level]}`}>{lesson.level}</span>
                  </td>
                  <td className="py-3 pr-4 text-wb-ink">{lesson.targetAgeGroup === 'Child' ? 'Children' : 'Adults'}</td>
                  <td className="py-3 pr-4 text-wb-ink">{lesson.type === 'Vocabulary' ? lesson.senseCount : '—'}</td>
                  <td className="py-3 pr-4">
                    <LessonStatusBadge isPublished={lesson.isPublished} />
                  </td>
                  <td className="py-3">
                    <div className="flex flex-wrap gap-2">
                      <Link to={`/admin/lessons/${lesson.id}`} className={softButtonClass}>
                        Edit
                      </Link>
                      <button
                        type="button"
                        disabled={setPublished.isPending}
                        onClick={() => setPublished.mutate({ id: lesson.id, publish: !lesson.isPublished })}
                        className={softButtonClass}
                      >
                        {lesson.isPublished ? 'Unpublish' : 'Publish'}
                      </button>
                      <button type="button" onClick={() => setToDelete(lesson)} className={dangerButtonClass}>
                        Delete
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <ConfirmDialog
        open={toDelete !== null}
        title={`Delete "${toDelete?.title ?? ''}"?`}
        message="The lesson and its word list are removed. The words stay in the catalogue."
        confirmLabel="Delete"
        pendingLabel="Deleting…"
        isPending={remove.isPending}
        errorMessage={remove.isError ? problemMessage(remove.error, 'Could not delete the lesson.') : null}
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
