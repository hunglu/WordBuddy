import { zodResolver } from '@hookform/resolvers/zod'
import { motion } from 'framer-motion'
import { useMemo, useState, type ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import type { TFunction } from 'i18next'
import { useForm, useWatch } from 'react-hook-form'
import { z } from 'zod'
import { useUpdatePersonalContext } from '../../hooks/useVocabulary'
import { PersonalContextField } from './PersonalContextField'

function makeNoteSchema(t: TFunction<'vocabulary'>) {
  return z.object({
    personalContext: z.string().max(500, t('validation.noteMax')),
  })
}

type NoteFormValues = z.infer<ReturnType<typeof makeNoteSchema>>

type PersonalContextEditorProps = Readonly<{
  wordId: string
  /** The saved note, or `null` when there is none. */
  note: string | null
}>

/** Shows my private note on a My words card, with Add / Edit, Save and Clear. */
export function PersonalContextEditor({ wordId, note }: PersonalContextEditorProps): ReactElement {
  const { t } = useTranslation(['vocabulary', 'common'])
  const noteSchema = useMemo(() => makeNoteSchema(t), [t])
  const [editing, setEditing] = useState(false)
  const update = useUpdatePersonalContext()
  const {
    register,
    handleSubmit,
    reset,
    control,
    formState: { errors },
  } = useForm<NoteFormValues>({
    resolver: zodResolver(noteSchema),
    defaultValues: { personalContext: note ?? '' },
  })
  const draft = useWatch({ control, name: 'personalContext' }) ?? ''

  const open = (): void => {
    update.reset()
    reset({ personalContext: note ?? '' })
    setEditing(true)
  }

  const save = handleSubmit((values) => {
    update.mutate(
      { id: wordId, personalContext: values.personalContext.trim() || null },
      { onSuccess: () => setEditing(false) },
    )
  })

  const clear = (): void => {
    update.mutate({ id: wordId, personalContext: null }, { onSuccess: () => setEditing(false) })
  }

  if (!editing) {
    return (
      <div className="flex flex-col gap-1">
        {note && (
          <p className="rounded-wb-md bg-wb-hover-tint px-3 py-2 text-sm italic text-wb-ink">
            <span className="font-semibold not-italic">{t('vocabulary:note.mine')}</span>
            {note}
          </p>
        )}
        <button
          type="button"
          onClick={open}
          className="self-start rounded-wb-md bg-wb-primary-soft px-3 py-1.5 text-sm font-semibold text-wb-ink hover:bg-wb-primary-soft-hover"
        >
          {note ? t('vocabulary:note.edit') : t('vocabulary:note.add')}
        </button>
      </div>
    )
  }

  return (
    <motion.form
      initial={{ opacity: 0, y: -8 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.2 }}
      onSubmit={save}
      aria-label={t('vocabulary:note.edit')}
      className="flex flex-col gap-2"
    >
      <PersonalContextField
        id={`note-${wordId}`}
        value={draft}
        error={errors.personalContext?.message}
        {...register('personalContext')}
      />
      {update.isError && (
        <p role="alert" className="text-sm text-wb-danger">
          {t('vocabulary:note.saveFailed')}
        </p>
      )}
      <div className="flex gap-2">
        <button
          type="submit"
          disabled={update.isPending}
          className="rounded-wb-md bg-wb-primary px-4 py-1.5 text-sm font-bold text-wb-on-primary shadow-wb-card hover:bg-wb-primary-hover disabled:opacity-60"
        >
          {update.isPending ? t('common:saving') : t('common:save')}
        </button>
        {note && (
          <button
            type="button"
            onClick={clear}
            disabled={update.isPending}
            className="rounded-wb-md bg-wb-danger-soft px-4 py-1.5 text-sm font-semibold text-wb-danger-soft-ink hover:bg-wb-danger-soft-hover disabled:opacity-60"
          >
            {t('vocabulary:note.clear')}
          </button>
        )}
        <button
          type="button"
          onClick={() => setEditing(false)}
          className="rounded-wb-md bg-wb-primary-soft px-4 py-1.5 text-sm font-semibold text-wb-ink hover:bg-wb-primary-soft-hover"
        >
          {t('common:cancel')}
        </button>
      </div>
    </motion.form>
  )
}
