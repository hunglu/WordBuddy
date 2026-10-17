import axios from 'axios'

/** Whether an API error is a 403 (not the owner / no access). */
export function isForbidden(error: unknown): boolean {
  return axios.isAxiosError(error) && error.response?.status === 403
}

export const inlineErrorClass = 'text-sm text-wb-danger'
