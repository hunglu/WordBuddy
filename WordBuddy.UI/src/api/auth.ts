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
  /** Current UI language code. Optional. */
  preferredLanguage?: string
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

/** Sets (or clears with `null`) the caller preferred UI language. Returns the fresh profile. */
export async function updatePreferredLanguage(language: string | null): Promise<User> {
  const { data } = await apiClient.put<User>('/auth/profile/language', { language })
  return data
}

/** Sets (or clears with `null`) the caller date of birth (`yyyy-MM-dd`). Returns a fresh token with the new age band. */
export async function updateDateOfBirth(dateOfBirth: string | null): Promise<AuthToken> {
  const { data } = await apiClient.put<AuthToken>('/auth/profile/date-of-birth', { dateOfBirth })
  return data
}

/** Changes the caller display name. Returns the fresh profile. */
export async function updateDisplayName(displayName: string): Promise<User> {
  const { data } = await apiClient.put<User>('/auth/profile/display-name', { displayName })
  return data
}

export interface ChangePasswordPayload {
  currentPassword: string
  newPassword: string
}

/** Changes the caller password. Returns a fresh token. */
export async function changePassword(payload: ChangePasswordPayload): Promise<AuthToken> {
  const { data } = await apiClient.post<AuthToken>('/auth/profile/password', payload)
  return data
}
