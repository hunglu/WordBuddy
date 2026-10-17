import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import { useMemo, type ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import type { TFunction } from 'i18next'
import { useForm } from 'react-hook-form'
import { Link, useNavigate } from 'react-router-dom'
import { z } from 'zod'
import { registerUser } from '../api/auth'
import { problemMessage } from '../api/supportLinks'
import { useAuthStore } from '../store/authStore'
import { browserTimeZone } from '../utils/timeZone'
import type { AgeGroup } from '../types'
import { useRetranslateFormErrors } from '../hooks/useRetranslateFormErrors'

function makeRegisterSchema(t: TFunction<'auth'>) {
  return z
    .object({
      displayName: z.string().min(1, t('validation.displayNameRequired')).max(100),
      email: z.string().email(t('validation.emailInvalid')),
      password: z.string().min(8, t('validation.passwordMin')),
      confirmPassword: z.string(),
      ageGroup: z.enum(['Child', 'Adult'], { message: t('validation.chooseOne') }),
    })
    .refine((values) => values.password === values.confirmPassword, {
      message: t('validation.passwordMismatch'),
      path: ['confirmPassword'],
    })
}

type RegisterFormValues = z.infer<ReturnType<typeof makeRegisterSchema>>

const AGE_GROUPS: { value: AgeGroup; labelKey: 'register.child' | 'register.adult'; emoji: string }[] = [
  { value: 'Child', labelKey: 'register.child', emoji: '🧒' },
  { value: 'Adult', labelKey: 'register.adult', emoji: '🧑' },
]

export function RegisterPage(): ReactElement {
  const { t, i18n } = useTranslation('auth')
  const navigate = useNavigate()
  const registerSchema = useMemo(() => makeRegisterSchema(t), [t])
  const login = useAuthStore((state) => state.login)

  const {
    register,
    handleSubmit,
    trigger,
    setValue,
    watch,
    formState: { errors },
  } = useForm<RegisterFormValues>({ resolver: zodResolver(registerSchema) })
  useRetranslateFormErrors(errors, trigger)

  const ageGroup = watch('ageGroup')

  const mutation = useMutation({
    mutationFn: registerUser,
    onSuccess: (auth) => {
      login(auth)
      navigate('/')
    },
  })

  const errorMessage: string | null = mutation.isError ? problemMessage(mutation.error, t('register.failed')) : null

  return (
    <motion.div
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3 }}
    >
      <h2 className="mb-4 text-center text-lg font-semibold text-wb-ink">{t('register.join')}</h2>

      <form
        onSubmit={handleSubmit((values) =>
          mutation.mutate({
            email: values.email,
            password: values.password,
            displayName: values.displayName,
            ageGroup: values.ageGroup,
            timeZoneId: browserTimeZone(),
            preferredLanguage: i18n.language,
          }),
        )}
        className="flex flex-col gap-4"
      >
        <div>
          <label htmlFor="displayName" className="mb-1 block text-sm font-semibold text-wb-ink">
            {t('register.name')}
          </label>
          <input
            id="displayName"
            className="w-full rounded-wb-md border-2 border-wb-border-control px-4 py-3 text-base focus:border-wb-primary"
            {...register('displayName')}
          />
          {errors.displayName && (
            <p className="mt-1 text-sm text-wb-danger">{errors.displayName.message}</p>
          )}
        </div>

        <div>
          <label htmlFor="email" className="mb-1 block text-sm font-semibold text-wb-ink">
            {t('register.email')}
          </label>
          <input
            id="email"
            type="email"
            autoComplete="email"
            className="w-full rounded-wb-md border-2 border-wb-border-control px-4 py-3 text-base focus:border-wb-primary"
            {...register('email')}
          />
          {errors.email && <p className="mt-1 text-sm text-wb-danger">{errors.email.message}</p>}
        </div>

        <div>
          <label htmlFor="password" className="mb-1 block text-sm font-semibold text-wb-ink">
            {t('register.password')}
          </label>
          <input
            id="password"
            type="password"
            autoComplete="new-password"
            className="w-full rounded-wb-md border-2 border-wb-border-control px-4 py-3 text-base focus:border-wb-primary"
            {...register('password')}
          />
          {errors.password && (
            <p className="mt-1 text-sm text-wb-danger">{errors.password.message}</p>
          )}
        </div>

        <div>
          <label htmlFor="confirmPassword" className="mb-1 block text-sm font-semibold text-wb-ink">
            {t('register.confirmPassword')}
          </label>
          <input
            id="confirmPassword"
            type="password"
            autoComplete="new-password"
            className="w-full rounded-wb-md border-2 border-wb-border-control px-4 py-3 text-base focus:border-wb-primary"
            {...register('confirmPassword')}
          />
          {errors.confirmPassword && (
            <p className="mt-1 text-sm text-wb-danger">{errors.confirmPassword.message}</p>
          )}
        </div>

        <div>
          <span className="mb-1 block text-sm font-semibold text-wb-ink">{t('register.iAm')}</span>
          <div className="grid grid-cols-2 gap-3">
            {AGE_GROUPS.map((option) => (
              <button
                key={option.value}
                type="button"
                onClick={() => setValue('ageGroup', option.value, { shouldValidate: true })}
                className={`flex flex-col items-center gap-1 rounded-wb-lg border-2 py-4 text-lg font-bold ${
                  ageGroup === option.value
                    ? 'border-wb-primary bg-wb-surface-page text-wb-ink-muted'
                    : 'border-wb-border-subtle text-wb-ink hover:bg-wb-surface-page'
                }`}
              >
                <span className="text-3xl">{option.emoji}</span>
                {t(option.labelKey)}
              </button>
            ))}
          </div>
          {errors.ageGroup && (
            <p className="mt-1 text-sm text-wb-danger">{errors.ageGroup.message}</p>
          )}
        </div>

        {errorMessage && <p className="text-sm text-wb-danger">{errorMessage}</p>}

        <button
          type="submit"
          disabled={mutation.isPending}
          className="rounded-wb-md bg-wb-primary py-3 text-lg font-bold text-wb-on-primary shadow-wb-card hover:bg-wb-primary-hover disabled:opacity-60"
        >
          {mutation.isPending ? t('register.submitting') : t('register.submit')}
        </button>
      </form>

      <p className="mt-4 text-center text-sm text-wb-ink">
        {t('register.haveAccount')}{' '}
        <Link to="/login" className="font-semibold text-wb-ink-muted hover:underline">
          {t('register.logIn')}
        </Link>
      </p>
    </motion.div>
  )
}
