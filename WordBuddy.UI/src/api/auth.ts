import type { AgeGroup, AuthToken } from '../types'
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
}

export async function loginUser(payload: LoginPayload): Promise<AuthToken> {
  const { data } = await apiClient.post<AuthToken>('/auth/login', payload)
  return data
}

export async function registerUser(payload: RegisterPayload): Promise<AuthToken> {
  const { data } = await apiClient.post<AuthToken>('/auth/register', payload)
  return data
}
