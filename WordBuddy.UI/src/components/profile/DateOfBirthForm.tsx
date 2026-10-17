import { zodResolver } from '@hookform/resolvers/zod'
import { useMemo, type ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import type { TFunction } from 'i18next'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { problemMessage } from '../../api/supportLinks'
import { useUpdateDateOfBirth } from '../../hooks/useProfile'
import type { User } from '../../types'
import { cardClass, inputClass, primaryButtonClass } from '../support/supportUi'

/** Today as `yyyy-MM-dd` in the browser time zone. */
function todayIso(): string {
  const now = new Date()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${now.getFullYear()}-${month}-${day}`
}

function makeDateOfBirthSchema(t: TFunction<'profile'>) {
  return z.object({
    // Empty clears the date. ISO dates compare as strings, so no time zone shift can change the result.
    dateOfBirth: z
      .string()
      .refine((value) => value.length === 0 || value <= todayIso(), t('validation.futureDate')),
  })
}

type DateOfBirthFormValues = z.infer<ReturnType<typeof makeDateOfBirthSchema>>

type DateOfBirthFormProps = Readonly<{ user: User }>

/** Date of birth input. The server also checks the date against the account age group. */
export function DateOfBirthForm({ user }: DateOfBirthFormProps): ReactElement {
  const { t } = useTranslation(['profile', 'common'])
  const update = useUpdateDateOfBirth()
  const dateOfBirthSchema = useMemo(() => makeDateOfBirthSchema(t), [t])
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<DateOfBirthFormValues>({
    resolver: zodResolver(dateOfBirthSchema),
    defaultValues: { dateOfBirth: user.dateOfBirth ?? '' },
  })
  const isChild = user.ageGroup === 'Child'

  return (
    <form
      className={`mt-6 max-w-xl ${cardClass}`}
      onSubmit={handleSubmit((values) => update.mutate(values.dateOfBirth.length === 0 ? null : values.dateOfBirth))}
      noValidate
    >
      <label htmlFor="dateOfBirth" className="block text-sm font-semibold text-wb-ink">
        {t('profile:dob.label')}
      </label>
      <p className="mt-1 text-sm text-wb-ink-muted">
        {isChild
          ? t('profile:dob.childHint')
          : t('profile:dob.adultHint')}
      </p>
      <input id="dateOfBirth" type="date" max={todayIso()} {...register('dateOfBirth')} className={`mt-2 ${inputClass}`} />
      {errors.dateOfBirth && <p className="mt-1 text-sm text-wb-danger">{errors.dateOfBirth.message}</p>}

      <button type="submit" disabled={update.isPending} className={`mt-4 block ${primaryButtonClass}`}>
        {t('profile:dob.save')}
      </button>
      {update.isSuccess && <p className="mt-2 text-sm text-wb-ink">{t('common:saved')}</p>}
      {update.isError && (
        <p className="mt-2 text-sm text-wb-danger">{problemMessage(update.error, t('profile:dob.saveFailed'))}</p>
      )}
    </form>
  )
}
