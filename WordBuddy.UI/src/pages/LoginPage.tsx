import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import { useMemo, type ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import type { TFunction } from 'i18next'
import { useForm } from 'react-hook-form'
import { Link, useNavigate } from 'react-router-dom'
import { z } from 'zod'
import { loginUser } from '../api/auth'
import { problemMessage } from '../api/supportLinks'
import { useAuthStore } from '../store/authStore'
import { useRetranslateFormErrors } from '../hooks/useRetranslateFormErrors'

function makeLoginSchema(t: TFunction<'auth'>) {
  return z.object({
    email: z.string().email(t('validation.emailInvalid')),
    password: z.string().min(1, t('validation.passwordRequired')),
  })
}

type LoginFormValues = z.infer<ReturnType<typeof makeLoginSchema>>

export function LoginPage(): ReactElement {
  const { t } = useTranslation('auth')
  const navigate = useNavigate()
  const loginSchema = useMemo(() => makeLoginSchema(t), [t])
  const login = useAuthStore((state) => state.login)

  const {
    register,
    handleSubmit,
    trigger,
    formState: { errors },
  } = useForm<LoginFormValues>({ resolver: zodResolver(loginSchema) })
  useRetranslateFormErrors(errors, trigger)

  const mutation = useMutation({
    mutationFn: loginUser,
    onSuccess: (auth) => {
      login(auth)
      navigate('/')
    },
  })

  const errorMessage: string | null = mutation.isError ? problemMessage(mutation.error, t('login.failed')) : null

  return (
    <motion.div
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3 }}
    >
      <h2 className="mb-4 text-center text-lg font-semibold text-wb-ink">{t('login.welcome')}</h2>

      <form onSubmit={handleSubmit((values) => mutation.mutate(values))} className="flex flex-col gap-4">
        <div>
          <label htmlFor="email" className="mb-1 block text-sm font-semibold text-wb-ink">
            {t('login.email')}
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
            {t('login.password')}
          </label>
          <input
            id="password"
            type="password"
            autoComplete="current-password"
            className="w-full rounded-wb-md border-2 border-wb-border-control px-4 py-3 text-base focus:border-wb-primary"
            {...register('password')}
          />
          {errors.password && (
            <p className="mt-1 text-sm text-wb-danger">{errors.password.message}</p>
          )}
        </div>

        {errorMessage && <p className="text-sm text-wb-danger">{errorMessage}</p>}

        <button
          type="submit"
          disabled={mutation.isPending}
          className="rounded-wb-md bg-wb-primary py-3 text-lg font-bold text-wb-on-primary shadow-wb-card hover:bg-wb-primary-hover disabled:opacity-60"
        >
          {mutation.isPending ? t('login.submitting') : t('login.submit')}
        </button>
      </form>

      <p className="mt-4 text-center text-sm text-wb-ink">
        {t('login.newHere')}{' '}
        <Link to="/register" className="font-semibold text-wb-ink-muted hover:underline">
          {t('login.createAccount')}
        </Link>
      </p>
    </motion.div>
  )
}
