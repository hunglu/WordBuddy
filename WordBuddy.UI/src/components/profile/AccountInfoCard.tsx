import { zodResolver } from '@hookform/resolvers/zod'
import type { ReactElement } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { problemMessage } from '../../api/supportLinks'
import { useUpdateDisplayName } from '../../hooks/useProfile'
import type { User } from '../../types'
import { cardClass, inputClass, primaryButtonClass } from '../support/supportUi'

const displayNameSchema = z.object({
  displayName: z.string().trim().min(2, 'Use at least 2 characters').max(50, 'Use at most 50 characters'),
})

type DisplayNameFormValues = z.infer<typeof displayNameSchema>

type AccountInfoCardProps = Readonly<{ user: User }>

/** Email and age group (read-only) and the display name form. A child display name is never shown to others. */
export function AccountInfoCard({ user }: AccountInfoCardProps): ReactElement {
  const update = useUpdateDisplayName()
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<DisplayNameFormValues>({
    resolver: zodResolver(displayNameSchema),
    defaultValues: { displayName: user.displayName },
  })
  const isChild = user.ageGroup === 'Child'

  return (
    <section className={`mt-6 max-w-xl ${cardClass}`} aria-labelledby="account-heading">
      <h2 id="account-heading" className="text-xl font-bold text-wb-ink">
        Account
      </h2>
      <dl className="mt-3 grid grid-cols-[auto_1fr] gap-x-4 gap-y-1 text-sm">
        <dt className="font-semibold text-wb-ink">Email</dt>
        <dd className="text-wb-ink-muted">{user.email}</dd>
        <dt className="font-semibold text-wb-ink">Age group</dt>
        <dd className="text-wb-ink-muted">{user.ageGroup}</dd>
      </dl>

      <form onSubmit={handleSubmit((values) => update.mutate(values.displayName))} noValidate>
        <label htmlFor="displayName" className="mt-4 block text-sm font-semibold text-wb-ink">
          Display name
        </label>
        <input id="displayName" maxLength={50} {...register('displayName')} className={inputClass} />
        {errors.displayName && <p className="mt-1 text-sm text-wb-danger">{errors.displayName.message}</p>}
        {isChild && <p className="mt-1 text-sm text-wb-ink-muted">Only you see this name. Other people see your alias.</p>}

        <button type="submit" disabled={update.isPending} className={`mt-4 block ${primaryButtonClass}`}>
          Save name
        </button>
        {update.isSuccess && <p className="mt-2 text-sm text-wb-ink">Saved.</p>}
        {update.isError && (
          <p className="mt-2 text-sm text-wb-danger">{problemMessage(update.error, 'Could not save the name.')}</p>
        )}
      </form>
    </section>
  )
}
