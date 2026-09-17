import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation } from '@tanstack/react-query'
import axios from 'axios'
import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate } from 'react-router-dom'
import { z } from 'zod'
import { registerUser } from '../api/auth'
import { useAuthStore } from '../store/authStore'
import type { AgeGroup } from '../types'

const registerSchema = z
  .object({
    displayName: z.string().min(1, 'Display name is required').max(100),
    email: z.string().email('Enter a valid email address'),
    password: z.string().min(8, 'Password must be at least 8 characters'),
    confirmPassword: z.string(),
    ageGroup: z.enum(['Child', 'Adult'], { message: 'Choose one' }),
  })
  .refine((values) => values.password === values.confirmPassword, {
    message: "Passwords don't match",
    path: ['confirmPassword'],
  })

type RegisterFormValues = z.infer<typeof registerSchema>

const AGE_GROUPS: { value: AgeGroup; label: string; emoji: string }[] = [
  { value: 'Child', label: 'Child', emoji: '🧒' },
  { value: 'Adult', label: 'Adult', emoji: '🧑' },
]

export function RegisterPage(): ReactElement {
  const navigate = useNavigate()
  const login = useAuthStore((state) => state.login)

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    formState: { errors },
  } = useForm<RegisterFormValues>({ resolver: zodResolver(registerSchema) })

  const ageGroup = watch('ageGroup')

  const mutation = useMutation({
    mutationFn: registerUser,
    onSuccess: (auth) => {
      login(auth)
      navigate('/')
    },
  })

  const errorMessage = axios.isAxiosError(mutation.error)
    ? ((mutation.error.response?.data as { detail?: string } | undefined)?.detail ??
      'Could not create your account.')
    : mutation.isError
      ? 'Something went wrong. Please try again.'
      : null

  return (
    <motion.div
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3 }}
    >
      <h2 className="mb-4 text-center text-lg font-semibold text-sky-900">Join WordBuddy!</h2>

      <form
        onSubmit={handleSubmit((values) =>
          mutation.mutate({
            email: values.email,
            password: values.password,
            displayName: values.displayName,
            ageGroup: values.ageGroup,
          }),
        )}
        className="flex flex-col gap-4"
      >
        <div>
          <label htmlFor="displayName" className="mb-1 block text-sm font-semibold text-sky-900">
            Name
          </label>
          <input
            id="displayName"
            className="w-full rounded-xl border-2 border-sky-200 px-4 py-3 text-base focus:border-sky-500 focus:outline-none"
            {...register('displayName')}
          />
          {errors.displayName && (
            <p className="mt-1 text-sm text-rose-600">{errors.displayName.message}</p>
          )}
        </div>

        <div>
          <label htmlFor="email" className="mb-1 block text-sm font-semibold text-sky-900">
            Email
          </label>
          <input
            id="email"
            type="email"
            autoComplete="email"
            className="w-full rounded-xl border-2 border-sky-200 px-4 py-3 text-base focus:border-sky-500 focus:outline-none"
            {...register('email')}
          />
          {errors.email && <p className="mt-1 text-sm text-rose-600">{errors.email.message}</p>}
        </div>

        <div>
          <label htmlFor="password" className="mb-1 block text-sm font-semibold text-sky-900">
            Password
          </label>
          <input
            id="password"
            type="password"
            autoComplete="new-password"
            className="w-full rounded-xl border-2 border-sky-200 px-4 py-3 text-base focus:border-sky-500 focus:outline-none"
            {...register('password')}
          />
          {errors.password && (
            <p className="mt-1 text-sm text-rose-600">{errors.password.message}</p>
          )}
        </div>

        <div>
          <label htmlFor="confirmPassword" className="mb-1 block text-sm font-semibold text-sky-900">
            Confirm password
          </label>
          <input
            id="confirmPassword"
            type="password"
            autoComplete="new-password"
            className="w-full rounded-xl border-2 border-sky-200 px-4 py-3 text-base focus:border-sky-500 focus:outline-none"
            {...register('confirmPassword')}
          />
          {errors.confirmPassword && (
            <p className="mt-1 text-sm text-rose-600">{errors.confirmPassword.message}</p>
          )}
        </div>

        <div>
          <span className="mb-1 block text-sm font-semibold text-sky-900">I am a…</span>
          <div className="grid grid-cols-2 gap-3">
            {AGE_GROUPS.map((option) => (
              <button
                key={option.value}
                type="button"
                onClick={() => setValue('ageGroup', option.value, { shouldValidate: true })}
                className={`flex flex-col items-center gap-1 rounded-2xl border-2 py-4 text-lg font-bold transition-colors ${
                  ageGroup === option.value
                    ? 'border-sky-500 bg-sky-50 text-sky-700'
                    : 'border-sky-200 text-sky-900 hover:bg-sky-50'
                }`}
              >
                <span className="text-3xl">{option.emoji}</span>
                {option.label}
              </button>
            ))}
          </div>
          {errors.ageGroup && (
            <p className="mt-1 text-sm text-rose-600">{errors.ageGroup.message}</p>
          )}
        </div>

        {errorMessage && <p className="text-sm text-rose-600">{errorMessage}</p>}

        <button
          type="submit"
          disabled={mutation.isPending}
          className="rounded-xl bg-sky-500 py-3 text-lg font-bold text-white shadow transition-colors hover:bg-sky-600 disabled:opacity-60"
        >
          {mutation.isPending ? 'Creating account…' : 'Create account'}
        </button>
      </form>

      <p className="mt-4 text-center text-sm text-sky-900">
        Already have an account?{' '}
        <Link to="/login" className="font-semibold text-sky-600 hover:underline">
          Log in
        </Link>
      </p>
    </motion.div>
  )
}
