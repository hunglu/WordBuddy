import { zodResolver } from '@hookform/resolvers/zod'
import { motion } from 'framer-motion'
import { useMemo, type ReactElement } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import type { TFunction } from 'i18next'
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
import { useRetranslateFormErrors } from '../hooks/useRetranslateFormErrors'

function makeLessonSchema(t: TFunction<'admin'>) {
  return z.object({
    title: z.string().trim().min(1, t('lessonEditor.validation.titleRequired')).max(200, t('lessonEditor.validation.titleMax')),
    description: z
      .string()
      .trim()
      .min(1, t('lessonEditor.validation.descriptionRequired'))
      .max(2000, t('lessonEditor.validation.descriptionMax')),
    type: z.enum(['Vocabulary', 'Grammar', 'DailyPhrase']),
    level: z.enum(['Beginner', 'Intermediate', 'Advanced']),
    targetAgeGroup: z.enum(['Child', 'Adult']),
  })
}

const DEFAULT_VALUES: LessonFormValues = {
  title: '',
  description: '',
  type: 'Vocabulary',
  level: 'Beginner',
  targetAgeGroup: 'Child',
}

/** `/admin/lessons/new` and `/admin/lessons/:id` — create or edit a lesson and its words. Admin or moderator. */
export function AdminLessonEditorPage(): ReactElement {
  const { t } = useTranslation(['admin', 'lessons'])
  const lessonSchema = useMemo(() => makeLessonSchema(t), [t])
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
    trigger,
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
  useRetranslateFormErrors(errors, trigger)

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
        {t('admin:lessonEditor.back')}
      </Link>

      <div className="mt-2 flex flex-wrap items-center gap-3">
        <h1 className="text-3xl font-extrabold text-wb-ink">{isNew ? t('admin:lessonEditor.newTitle') : (data?.title ?? t('admin:lessonEditor.fallbackTitle'))}</h1>
        {data && <LessonStatusBadge isPublished={data.isPublished} />}
        {data && (
          <button
            type="button"
            disabled={setPublished.isPending}
            onClick={() => setPublished.mutate({ id: data.id, publish: !data.isPublished })}
            className={softButtonClass}
          >
            {data.isPublished ? t('admin:lessons.unpublish') : t('admin:lessons.publish')}
          </button>
        )}
      </div>
      {isNew && <p className="mt-1 text-wb-ink-muted">{t('admin:lessonEditor.newHint')}</p>}
      {setPublished.isError && (
        <p role="alert" className="mt-2 text-sm font-semibold text-wb-danger">
          {problemMessage(setPublished.error, t('admin:lessons.statusFailed'))}
        </p>
      )}

      {!isNew && lesson.isLoading && <p className="mt-8 text-lg text-wb-ink-muted">{t('admin:lessonEditor.loading')}</p>}
      {!isNew && lesson.isError && <p className="mt-8 text-lg text-wb-danger">{t('admin:lessonEditor.loadFailed')}</p>}

      {(isNew || data) && (
        <form onSubmit={handleSubmit(onSubmit)} className={`mt-6 grid gap-4 md:grid-cols-2 ${cardClass}`}>
          <div className="md:col-span-2">
            <label htmlFor="lesson-title" className="mb-1 block text-sm font-semibold text-wb-ink">
              {t('admin:lessonEditor.title')}
            </label>
            <input id="lesson-title" type="text" className={inputClass} {...register('title')} />
            {errors.title && <p className="mt-1 text-sm text-wb-danger">{errors.title.message}</p>}
          </div>

          <div className="md:col-span-2">
            <label htmlFor="lesson-description" className="mb-1 block text-sm font-semibold text-wb-ink">
              {t('admin:lessonEditor.description')}
            </label>
            <textarea id="lesson-description" rows={3} className={inputClass} {...register('description')} />
            {errors.description && <p className="mt-1 text-sm text-wb-danger">{errors.description.message}</p>}
          </div>

          <div>
            <label htmlFor="lesson-type" className="mb-1 block text-sm font-semibold text-wb-ink">
              {t('admin:lessonEditor.type')}
            </label>
            <select id="lesson-type" disabled={!isNew} className={inputClass} {...register('type')}>
              <option value="Vocabulary">{t('admin:lessons.type.Vocabulary')}</option>
              <option value="Grammar">{t('admin:lessons.type.Grammar')}</option>
              <option value="DailyPhrase">{t('admin:lessons.type.DailyPhrase')}</option>
            </select>
            {!isNew && <p className="mt-1 text-xs text-wb-ink-muted">{t('admin:lessonEditor.typeLocked')}</p>}
          </div>

          <div>
            <label htmlFor="lesson-level" className="mb-1 block text-sm font-semibold text-wb-ink">
              {t('admin:lessonEditor.level')}
            </label>
            <select id="lesson-level" className={inputClass} {...register('level')}>
              <option value="Beginner">{t('lessons:level.Beginner')}</option>
              <option value="Intermediate">{t('lessons:level.Intermediate')}</option>
              <option value="Advanced">{t('lessons:level.Advanced')}</option>
            </select>
          </div>

          <div>
            <label htmlFor="lesson-age" className="mb-1 block text-sm font-semibold text-wb-ink">
              {t('admin:lessonEditor.madeFor')}
            </label>
            <select id="lesson-age" className={inputClass} {...register('targetAgeGroup')}>
              <option value="Child">{t('admin:lessonEditor.forChild')}</option>
              <option value="Adult">{t('admin:lessonEditor.forAdult')}</option>
            </select>
          </div>

          <div className="flex items-end gap-3 md:col-span-2">
            <button type="submit" disabled={create.isPending || update.isPending || (!isNew && !isDirty)} className={primaryButtonClass}>
              {isNew ? t('admin:lessonEditor.createDraft') : t('admin:lessonEditor.saveChanges')}
            </button>
            {update.isSuccess && !isDirty && <span className="text-sm font-semibold text-wb-success">{t('admin:lessonEditor.saved')}</span>}
            {saveError !== null && saveError !== undefined && (
              <span role="alert" className="text-sm font-semibold text-wb-danger">
                {problemMessage(saveError, t('admin:lessonEditor.saveFailed'))}
              </span>
            )}
          </div>
        </form>
      )}

      {data?.type === 'Vocabulary' && <LessonSensesPanel lessonId={data.id} senses={data.senses} />}
      {data && data.type !== 'Vocabulary' && (
        <p className="mt-6 text-wb-ink-muted">{t('admin:lessonEditor.onlyVocabulary')}</p>
      )}
    </motion.div>
  )
}
