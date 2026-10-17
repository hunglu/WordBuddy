import { zodResolver } from '@hookform/resolvers/zod'
import { useMemo, type ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import type { TFunction } from 'i18next'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { problemMessage } from '../../api/supportLinks'
import { useChangePassword } from '../../hooks/useProfile'
import { cardClass, inputClass, primaryButtonClass } from '../support/supportUi'

function makeChangePasswordSchema(t: TFunction<'profile'>) {
  return z
    .object({
      currentPassword: z.string().min(1, t('validation.currentRequired')),
      newPassword: z.string().min(8, t('validation.passwordMin')).max(128, t('validation.passwordMax')),
      confirmPassword: z.string(),
    })
    .refine((values) => values.newPassword === values.confirmPassword, {
      message: t('validation.mismatch'),
      path: ['confirmPassword'],
    })
}

type ChangePasswordFormValues = z.infer<ReturnType<typeof makeChangePasswordSchema>>

/** Change password: current, new and confirm. Fields clear after a successful change. */
export function ChangePasswordForm(): ReactElement {
  const { t } = useTranslation('profile')
  const change = useChangePassword()
  const changePasswordSchema = useMemo(() => makeChangePasswordSchema(t), [t])
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<ChangePasswordFormValues>({
    resolver: zodResolver(changePasswordSchema),
    defaultValues: { currentPassword: '', newPassword: '', confirmPassword: '' },
  })

  return (
    <form
      className={`mt-6 max-w-xl ${cardClass}`}
      onSubmit={handleSubmit((values) =>
        change.mutate(
          { currentPassword: values.currentPassword, newPassword: values.newPassword },
          { onSuccess: () => reset() },
        ),
      )}
      noValidate
    >
      <h2 className="text-xl font-bold text-wb-ink">{t('password.heading')}</h2>

      <label htmlFor="currentPassword" className="mt-3 block text-sm font-semibold text-wb-ink">
        {t('password.current')}
      </label>
      <input
        id="currentPassword"
        type="password"
        autoComplete="current-password"
        {...register('currentPassword')}
        className={inputClass}
      />
      {errors.currentPassword && <p className="mt-1 text-sm text-wb-danger">{errors.currentPassword.message}</p>}

      <label htmlFor="newPassword" className="mt-3 block text-sm font-semibold text-wb-ink">
        {t('password.new')}
      </label>
      <input id="newPassword" type="password" autoComplete="new-password" {...register('newPassword')} className={inputClass} />
      {errors.newPassword && <p className="mt-1 text-sm text-wb-danger">{errors.newPassword.message}</p>}

      <label htmlFor="confirmPassword" className="mt-3 block text-sm font-semibold text-wb-ink">
        {t('password.confirm')}
      </label>
      <input
        id="confirmPassword"
        type="password"
        autoComplete="new-password"
        {...register('confirmPassword')}
        className={inputClass}
      />
      {errors.confirmPassword && <p className="mt-1 text-sm text-wb-danger">{errors.confirmPassword.message}</p>}

      <button type="submit" disabled={change.isPending} className={`mt-4 block ${primaryButtonClass}`}>
        {t('password.submit')}
      </button>
      {change.isSuccess && <p className="mt-2 text-sm text-wb-ink">{t('password.changed')}</p>}
      {change.isError && (
        <p className="mt-2 text-sm text-wb-danger">{problemMessage(change.error, t('password.failed'))}</p>
      )}
    </form>
  )
}
