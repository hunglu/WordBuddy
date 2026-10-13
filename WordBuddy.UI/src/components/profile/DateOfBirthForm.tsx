import { zodResolver } from '@hookform/resolvers/zod'
import type { ReactElement } from 'react'
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

const dateOfBirthSchema = z.object({
  // Empty clears the date. ISO dates compare as strings, so no time zone shift can change the result.
  dateOfBirth: z
    .string()
    .refine((value) => value.length === 0 || value <= todayIso(), 'The date cannot be in the future'),
})

type DateOfBirthFormValues = z.infer<typeof dateOfBirthSchema>

type DateOfBirthFormProps = Readonly<{ user: User }>

/** Date of birth input. The server also checks the date against the account age group. */
export function DateOfBirthForm({ user }: DateOfBirthFormProps): ReactElement {
  const update = useUpdateDateOfBirth()
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
        Date of birth
      </label>
      <p className="mt-1 text-sm text-wb-ink-muted">
        {isChild
          ? 'Ask your guardian if you are not sure. Your linked supporters can see it.'
          : 'Active supporters you are linked with can see it. It helps WordBuddy set the right pace.'}
      </p>
      <input id="dateOfBirth" type="date" max={todayIso()} {...register('dateOfBirth')} className={`mt-2 ${inputClass}`} />
      {errors.dateOfBirth && <p className="mt-1 text-sm text-wb-danger">{errors.dateOfBirth.message}</p>}

      <button type="submit" disabled={update.isPending} className={`mt-4 block ${primaryButtonClass}`}>
        Save date of birth
      </button>
      {update.isSuccess && <p className="mt-2 text-sm text-wb-ink">Saved.</p>}
      {update.isError && (
        <p className="mt-2 text-sm text-wb-danger">{problemMessage(update.error, 'Could not save the date of birth.')}</p>
      )}
    </form>
  )
}
