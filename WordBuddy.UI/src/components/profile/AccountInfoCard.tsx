import { zodResolver } from '@hookform/resolvers/zod'
import { useMemo, type ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import type { TFunction } from 'i18next'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { problemMessage } from '../../api/supportLinks'
import { useUpdateDisplayName } from '../../hooks/useProfile'
import type { User } from '../../types'
import { cardClass, inputClass, primaryButtonClass } from '../support/supportUi'
import { useRetranslateFormErrors } from '../../hooks/useRetranslateFormErrors'

function makeDisplayNameSchema(t: TFunction<'profile'>) {
  return z.object({
    displayName: z.string().trim().min(2, t('validation.nameMin')).max(50, t('validation.nameMax')),
  })
}

type DisplayNameFormValues = z.infer<ReturnType<typeof makeDisplayNameSchema>>

type AccountInfoCardProps = Readonly<{ user: User }>

/** Email and age group (read-only) and the display name form. A child display name is never shown to others. */
export function AccountInfoCard({ user }: AccountInfoCardProps): ReactElement {
  const { t } = useTranslation(['profile', 'common'])
  const update = useUpdateDisplayName()
  const displayNameSchema = useMemo(() => makeDisplayNameSchema(t), [t])
  const {
    register,
    handleSubmit,
    trigger,
    formState: { errors },
  } = useForm<DisplayNameFormValues>({
    resolver: zodResolver(displayNameSchema),
    defaultValues: { displayName: user.displayName },
  })
  useRetranslateFormErrors(errors, trigger)
  const isChild = user.ageGroup === 'Child'

  return (
    <section className={`mt-6 max-w-xl ${cardClass}`} aria-labelledby="account-heading">
      <h2 id="account-heading" className="text-xl font-bold text-wb-ink">
        {t('profile:account.heading')}
      </h2>
      <dl className="mt-3 grid grid-cols-[auto_1fr] gap-x-4 gap-y-1 text-sm">
        <dt className="font-semibold text-wb-ink">{t('profile:account.email')}</dt>
        <dd className="text-wb-ink-muted">{user.email}</dd>
        <dt className="font-semibold text-wb-ink">{t('profile:account.ageGroup')}</dt>
        <dd className="text-wb-ink-muted">{t(`common:ageGroup.${user.ageGroup}`)}</dd>
      </dl>

      <form onSubmit={handleSubmit((values) => update.mutate(values.displayName))} noValidate>
        <label htmlFor="displayName" className="mt-4 block text-sm font-semibold text-wb-ink">
          {t('profile:account.displayName')}
        </label>
        <input id="displayName" maxLength={50} {...register('displayName')} className={inputClass} />
        {errors.displayName && <p className="mt-1 text-sm text-wb-danger">{errors.displayName.message}</p>}
        {isChild && <p className="mt-1 text-sm text-wb-ink-muted">{t('profile:account.childNote')}</p>}

        <button type="submit" disabled={update.isPending} className={`mt-4 block ${primaryButtonClass}`}>
          {t('profile:account.saveName')}
        </button>
        {update.isSuccess && <p className="mt-2 text-sm text-wb-ink">{t('common:saved')}</p>}
        {update.isError && (
          <p className="mt-2 text-sm text-wb-danger">{problemMessage(update.error, t('profile:account.saveNameFailed'))}</p>
        )}
      </form>
    </section>
  )
}
