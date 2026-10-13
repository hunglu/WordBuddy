import { zodResolver } from '@hookform/resolvers/zod'
import type { ReactElement } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { problemMessage } from '../../api/supportLinks'
import { useChangePassword } from '../../hooks/useProfile'
import { cardClass, inputClass, primaryButtonClass } from '../support/supportUi'

const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, 'Enter your current password'),
    newPassword: z.string().min(8, 'Password must be at least 8 characters').max(128, 'Use at most 128 characters'),
    confirmPassword: z.string(),
  })
  .refine((values) => values.newPassword === values.confirmPassword, {
    message: "Passwords don't match",
    path: ['confirmPassword'],
  })

type ChangePasswordFormValues = z.infer<typeof changePasswordSchema>

/** Change password: current, new and confirm. Fields clear after a successful change. */
export function ChangePasswordForm(): ReactElement {
  const change = useChangePassword()
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
      <h2 className="text-xl font-bold text-wb-ink">Change password</h2>

      <label htmlFor="currentPassword" className="mt-3 block text-sm font-semibold text-wb-ink">
        Current password
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
        New password
      </label>
      <input id="newPassword" type="password" autoComplete="new-password" {...register('newPassword')} className={inputClass} />
      {errors.newPassword && <p className="mt-1 text-sm text-wb-danger">{errors.newPassword.message}</p>}

      <label htmlFor="confirmPassword" className="mt-3 block text-sm font-semibold text-wb-ink">
        Confirm new password
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
        Change password
      </button>
      {change.isSuccess && <p className="mt-2 text-sm text-wb-ink">Password changed.</p>}
      {change.isError && (
        <p className="mt-2 text-sm text-wb-danger">{problemMessage(change.error, 'Could not change the password.')}</p>
      )}
    </form>
  )
}
