import type { AgeGroup, AuthToken, User } from '../types'
import { apiClient } from './client'

export interface LoginPayload {
  email: string
  password: string
}

export interface RegisterPayload {
  email: string
  password: string
  displayName: string
  ageGroup: AgeGroup
  /** IANA time zone id of the browser. Optional. */
  timeZoneId?: string
}

export async function loginUser(payload: LoginPayload): Promise<AuthToken> {
  const { data } = await apiClient.post<AuthToken>('/auth/login', payload)
  return data
}

export async function registerUser(payload: RegisterPayload): Promise<AuthToken> {
  const { data } = await apiClient.post<AuthToken>('/auth/register', payload)
  return data
}

/** Sets (or clears with `null`) the caller IANA time zone. Returns the fresh profile. */
export async function updateTimeZone(timeZoneId: string | null): Promise<User> {
  const { data } = await apiClient.put<User>('/auth/profile/time-zone', { timeZoneId })
  return data
}
