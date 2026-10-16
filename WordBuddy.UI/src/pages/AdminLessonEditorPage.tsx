import { zodResolver } from '@hookform/resolvers/zod'
import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useForm } from 'react-hook-form'
import { Link, Navigate, useNavigate, useParams } from 'react-router-dom'
import { z } from 'zod'
import { problemMessage } from '../api/supportLinks'
import { LessonSensesPanel } from '../components/admin-lessons/LessonSensesPanel'
import { LessonStatusBadge } from '../components/admin-lessons/LessonStatusBadge'
import { cardClass, inputClass, primaryButtonClass, softButtonClass } from '../components/support/supportUi'
import { useAdminLesson, useCreateLesson, useSetLessonPublished, useUpdateLesson } from '../hooks/useAdminLessons'
import { useAuthStore } from '../store/authStore'
import { canManageContent } from '../utils/roles'
import type { LessonFormValues } from '../types'

const lessonSchema = z.object({
  title: z.string().trim().min(1, 'Title is required').max(200, 'Title can have up to 200 characters'),
  description: z.string().trim().min(1, 'Description is required').max(2000, 'Description can have up to 2000 characters'),
  type: z.enum(['Vocabulary', 'Grammar', 'DailyPhrase']),
  level: z.enum(['Beginner', 'Intermediate', 'Advanced']),
  targetAgeGroup: z.enum(['Child', 'Adult']),
})

const DEFAULT_VALUES: LessonFormValues = {
  title: '',
  description: '',
  type: 'Vocabulary',
  level: 'Beginner',
  targetAgeGroup: 'Child',
}

/** `/admin/lessons/new` and `/admin/lessons/:id` — create or edit a lesson and its words. Admin or moderator. */
export function AdminLessonEditorPage(): ReactElement {
  const { id } = useParams<{ id: string }>()
  const isNew: boolean = id === undefined
  const navigate = useNavigate()
  const canManage: boolean = canManageContent(useAuthStore((state) => state.user))

  const lesson = useAdminLesson(id, canManage)
  const create = useCreateLesson()
  const update = useUpdateLesson()
  const setPublished = useSetLessonPublished()

  const {
    register,
    handleSubmit,
    formState: { errors, isDirty },
  } = useForm<LessonFormValues>({
    resolver: zodResolver(lessonSchema),
    defaultValues: DEFAULT_VALUES,
    // Fills the form once the lesson loads (edit mode) and again after every save.
    values: lesson.data
      ? {
          title: lesson.data.title,
          description: lesson.data.description,
          type: lesson.data.type,
          level: lesson.data.level,
          targetAgeGroup: lesson.data.targetAgeGroup,
        }
      : undefined,
  })

  // Server-side, `ManageContent` already rejects a non-admin call — this is a UX guard.
  if (!canManage) {
    return <Navigate to="/" replace />
  }

  const onSubmit = (values: LessonFormValues): void => {
    if (isNew) {
      create.mutate(values, { onSuccess: (newId) => navigate(`/admin/lessons/${newId}`, { replace: true }) })
      return
    }
    const { type: _type, ...editable } = values
    update.mutate({ id: id as string, values: editable })
  }

  const saveError: unknown = create.error ?? update.error
  const data = lesson.data

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <Link to="/admin/lessons" className="text-sm font-semibold text-wb-ink-muted hover:underline">
        ← All lessons
      </Link>

      <div className="mt-2 flex flex-wrap items-center gap-3">
        <h1 className="text-3xl font-extrabold text-wb-ink">{isNew ? 'New lesson' : (data?.title ?? 'Lesson')}</h1>
        {data && <LessonStatusBadge isPublished={data.isPublished} />}
        {data && (
          <button
            type="button"
            disabled={setPublished.isPending}
            onClick={() => setPublished.mutate({ id: data.id, publish: !data.isPublished })}
            className={softButtonClass}
          >
            {data.isPublished ? 'Unpublish' : 'Publish'}
          </button>
        )}
      </div>
      {isNew && <p className="mt-1 text-wb-ink-muted">New lessons start as drafts. Add words after you save, then publish.</p>}
      {setPublished.isError && (
        <p role="alert" className="mt-2 text-sm font-semibold text-wb-danger">
          {problemMessage(setPublished.error, 'Could not change the status.')}
        </p>
      )}

      {!isNew && lesson.isLoading && <p className="mt-8 text-lg text-wb-ink-muted">Loading the lesson…</p>}
      {!isNew && lesson.isError && <p className="mt-8 text-lg text-wb-danger">Couldn't load this lesson right now.</p>}

      {(isNew || data) && (
        <form onSubmit={handleSubmit(onSubmit)} className={`mt-6 grid gap-4 md:grid-cols-2 ${cardClass}`}>
          <div className="md:col-span-2">
            <label htmlFor="lesson-title" className="mb-1 block text-sm font-semibold text-wb-ink">
              Title
            </label>
            <input id="lesson-title" type="text" className={inputClass} {...register('title')} />
            {errors.title && <p className="mt-1 text-sm text-wb-danger">{errors.title.message}</p>}
          </div>

          <div className="md:col-span-2">
            <label htmlFor="lesson-description" className="mb-1 block text-sm font-semibold text-wb-ink">
              Description
            </label>
            <textarea id="lesson-description" rows={3} className={inputClass} {...register('description')} />
            {errors.description && <p className="mt-1 text-sm text-wb-danger">{errors.description.message}</p>}
          </div>

          <div>
            <label htmlFor="lesson-type" className="mb-1 block text-sm font-semibold text-wb-ink">
              Type
            </label>
            <select id="lesson-type" disabled={!isNew} className={inputClass} {...register('type')}>
              <option value="Vocabulary">Vocabulary</option>
              <option value="Grammar">Grammar</option>
              <option value="DailyPhrase">Daily phrase</option>
            </select>
            {!isNew && <p className="mt-1 text-xs text-wb-ink-muted">The type cannot change after the lesson is created.</p>}
          </div>

          <div>
            <label htmlFor="lesson-level" className="mb-1 block text-sm font-semibold text-wb-ink">
              Level
            </label>
            <select id="lesson-level" className={inputClass} {...register('level')}>
              <option value="Beginner">Beginner</option>
              <option value="Intermediate">Intermediate</option>
              <option value="Advanced">Advanced</option>
            </select>
          </div>

          <div>
            <label htmlFor="lesson-age" className="mb-1 block text-sm font-semibold text-wb-ink">
              Made for
            </label>
            <select id="lesson-age" className={inputClass} {...register('targetAgeGroup')}>
              <option value="Child">Children (adults see it too)</option>
              <option value="Adult">Adults only (children cannot open it)</option>
            </select>
          </div>

          <div className="flex items-end gap-3 md:col-span-2">
            <button type="submit" disabled={create.isPending || update.isPending || (!isNew && !isDirty)} className={primaryButtonClass}>
              {isNew ? 'Create draft' : 'Save changes'}
            </button>
            {update.isSuccess && !isDirty && <span className="text-sm font-semibold text-wb-success">Saved</span>}
            {saveError !== null && saveError !== undefined && (
              <span role="alert" className="text-sm font-semibold text-wb-danger">
                {problemMessage(saveError, 'Could not save the lesson.')}
              </span>
            )}
          </div>
        </form>
      )}

      {data?.type === 'Vocabulary' && <LessonSensesPanel lessonId={data.id} senses={data.senses} />}
      {data && data.type !== 'Vocabulary' && (
        <p className="mt-6 text-wb-ink-muted">Words can only be added to vocabulary lessons.</p>
      )}
    </motion.div>
  )
}
