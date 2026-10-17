import { zodResolver } from '@hookform/resolvers/zod'
import { motion } from 'framer-motion'
import { useMemo, type ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import type { TFunction } from 'i18next'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { problemCode } from '../../api/supportLinks'
import { useUpdateMyWord } from '../../hooks/useVocabulary'
import type { PersonalVocabularyWord } from '../../types'
import { useRetranslateFormErrors } from '../../hooks/useRetranslateFormErrors'

function makeEditSchema(t: TFunction<'vocabulary'>) {
  return z.object({
    word: z.string().min(1, t('validation.wordRequired')).max(200),
    definition: z.string().min(1, t('validation.definitionRequired')).max(2000),
    example: z.string().max(500).optional(),
  })
}

type EditFormValues = z.infer<ReturnType<typeof makeEditSchema>>

interface EditWordFormProps {
  word: PersonalVocabularyWord
  onClose: () => void
}

/** Inline form to edit one of my own private words. Saves in place; closes on success. */
export function EditWordForm({ word, onClose }: EditWordFormProps): ReactElement {
  const { t } = useTranslation(['vocabulary', 'common'])
  const updateWord = useUpdateMyWord()
  const editSchema = useMemo(() => makeEditSchema(t), [t])
  const {
    register,
    handleSubmit,
    trigger,
    formState: { errors },
  } = useForm<EditFormValues>({
    resolver: zodResolver(editSchema),
    defaultValues: { word: word.word, definition: word.definition, example: word.example ?? '' },
  })
  useRetranslateFormErrors(errors, trigger)

  const onSubmit = handleSubmit((values) => {
    updateWord.mutate(
      { id: word.id, payload: { word: values.word, definition: values.definition, example: values.example || undefined } },
      { onSuccess: onClose },
    )
  })

  const code = problemCode(updateWord.error)
  let errorMessage: string = t('vocabulary:edit.saveFailed')
  if (code === 'Sense.NotEditable') {
    errorMessage = t('vocabulary:edit.notEditable')
  } else if (code === 'PersonalVocabularyWord.Duplicate') {
    errorMessage = t('vocabulary:edit.duplicate')
  } else if (code === 'PersonalVocabularyWord.ConcurrentAdd') {
    errorMessage = t('vocabulary:edit.concurrent')
  }

  return (
    <motion.form
      initial={{ opacity: 0, y: -8 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.2 }}
      onSubmit={onSubmit}
      aria-label={t('vocabulary:edit.ariaLabel', { word: word.word })}
      className="flex flex-col gap-3"
    >
      <div>
        <label htmlFor={`edit-word-${word.id}`} className="mb-1 block text-sm font-semibold text-wb-ink">
          {t('vocabulary:edit.word')}
        </label>
        <input
          id={`edit-word-${word.id}`}
          className="w-full rounded-wb-md border-2 border-wb-border-control px-3 py-2 text-base focus:border-wb-primary"
          {...register('word')}
        />
        {errors.word && <p className="mt-1 text-sm text-wb-danger">{errors.word.message}</p>}
      </div>

      <div>
        <label htmlFor={`edit-definition-${word.id}`} className="mb-1 block text-sm font-semibold text-wb-ink">
          {t('vocabulary:edit.definition')}
        </label>
        <textarea
          id={`edit-definition-${word.id}`}
          rows={2}
          className="w-full rounded-wb-md border-2 border-wb-border-control px-3 py-2 text-base focus:border-wb-primary"
          {...register('definition')}
        />
        {errors.definition && <p className="mt-1 text-sm text-wb-danger">{errors.definition.message}</p>}
      </div>

      <div>
        <label htmlFor={`edit-example-${word.id}`} className="mb-1 block text-sm font-semibold text-wb-ink">
          {t('vocabulary:edit.example')}
        </label>
        <textarea
          id={`edit-example-${word.id}`}
          rows={2}
          className="w-full rounded-wb-md border-2 border-wb-border-control px-3 py-2 text-base focus:border-wb-primary"
          {...register('example')}
        />
        {errors.example && <p className="mt-1 text-sm text-wb-danger">{errors.example.message}</p>}
      </div>

      {updateWord.isError && (
        <p role="alert" className="text-sm text-wb-danger">
          {errorMessage}
        </p>
      )}

      <div className="flex gap-2">
        <button
          type="submit"
          disabled={updateWord.isPending}
          className="rounded-wb-md bg-wb-primary px-4 py-1.5 text-sm font-bold text-wb-on-primary shadow-wb-card hover:bg-wb-primary-hover disabled:opacity-60"
        >
          {updateWord.isPending ? t('common:saving') : t('common:save')}
        </button>
        <button
          type="button"
          onClick={onClose}
          className="rounded-wb-md bg-wb-primary-soft px-4 py-1.5 text-sm font-semibold text-wb-ink hover:bg-wb-primary-soft-hover"
        >
          {t('common:cancel')}
        </button>
      </div>
    </motion.form>
  )
}
