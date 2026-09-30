import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation } from '@tanstack/react-query'
import axios from 'axios'
import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate } from 'react-router-dom'
import { z } from 'zod'
import { loginUser } from '../api/auth'
import { useAuthStore } from '../store/authStore'

const loginSchema = z.object({
  email: z.string().email('Enter a valid email address'),
  password: z.string().min(1, 'Password is required'),
})

type LoginFormValues = z.infer<typeof loginSchema>

export function LoginPage(): ReactElement {
  const navigate = useNavigate()
  const login = useAuthStore((state) => state.login)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormValues>({ resolver: zodResolver(loginSchema) })

  const mutation = useMutation({
    mutationFn: loginUser,
    onSuccess: (auth) => {
      login(auth)
      navigate('/')
    },
  })

  const errorMessage = axios.isAxiosError(mutation.error)
    ? ((mutation.error.response?.data as { detail?: string } | undefined)?.detail ??
      'Email or password is incorrect.')
    : mutation.isError
      ? 'Something went wrong. Please try again.'
      : null

  return (
    <motion.div
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3 }}
    >
      <h2 className="mb-4 text-center text-lg font-semibold text-sky-900">Welcome back!</h2>

      <form onSubmit={handleSubmit((values) => mutation.mutate(values))} className="flex flex-col gap-4">
        <div>
          <label htmlFor="email" className="mb-1 block text-sm font-semibold text-sky-900">
            Email
          </label>
          <input
            id="email"
            type="email"
            autoComplete="email"
            className="w-full rounded-xl border-2 border-sky-200 px-4 py-3 text-base focus:border-sky-500"
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
            autoComplete="current-password"
            className="w-full rounded-xl border-2 border-sky-200 px-4 py-3 text-base focus:border-sky-500"
            {...register('password')}
          />
          {errors.password && (
            <p className="mt-1 text-sm text-rose-600">{errors.password.message}</p>
          )}
        </div>

        {errorMessage && <p className="text-sm text-rose-600">{errorMessage}</p>}

        <button
          type="submit"
          disabled={mutation.isPending}
          className="rounded-xl bg-sky-500 py-3 text-lg font-bold text-white shadow transition-colors hover:bg-sky-600 disabled:opacity-60"
        >
          {mutation.isPending ? 'Logging in…' : 'Log in'}
        </button>
      </form>

      <p className="mt-4 text-center text-sm text-sky-900">
        New here?{' '}
        <Link to="/register" className="font-semibold text-sky-600 hover:underline">
          Create an account
        </Link>
      </p>
    </motion.div>
  )
}
