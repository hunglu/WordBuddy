import axios, { type InternalAxiosRequestConfig } from 'axios'
import { useAuthStore } from '../store/authStore'

// Relative base URL — the Vite dev proxy (see vite.config.ts) and the Kubernetes ingress
// planned for Phase 4 both route /api/* by path prefix to the right backend service, so the
// frontend never needs to know a service's actual origin.
export const apiClient = axios.create({ baseURL: '/api' })

apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = useAuthStore.getState().token
  if (token) {
    config.headers.set('Authorization', `Bearer ${token}`)
  }
  return config
})

apiClient.interceptors.response.use(
  (response) => response,
  (error: unknown) => {
    if (axios.isAxiosError(error) && error.response?.status === 401) {
      useAuthStore.getState().logout()
      window.location.href = '/login'
    }
    return Promise.reject(error)
  },
)
