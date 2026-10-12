import type { Challenge, ChallengeBoard } from '../types'
import { apiClient } from './client'

/** Problem code: the challenge has no room left. */
export const CHALLENGE_FULL_CODE = 'Challenge.Full'

/** Problem code: a child needs Guardian consent first. */
export const CHALLENGE_NO_CONSENT_CODE = 'Challenge.NoConsent'

/** Open and running challenges of the caller audience. A child without consent gets an empty list. */
export async function listChallenges(): Promise<Challenge[]> {
  const { data } = await apiClient.get<Challenge[]>('/progress/challenges')
  return data
}

/** The board of one challenge. The server filters it for the caller (adult: full, child: own rank + top 5). */
export async function getChallengeBoard(id: string): Promise<ChallengeBoard> {
  const { data } = await apiClient.get<ChallengeBoard>(`/progress/challenges/${id}/board`)
  return data
}

export async function joinChallenge(id: string): Promise<void> {
  await apiClient.post(`/progress/challenges/${id}/join`)
}

export async function leaveChallenge(id: string): Promise<void> {
  await apiClient.delete(`/progress/challenges/${id}/join`)
}

/** A Guardian allows (or stops) challenges for a linked child. */
export async function setChildChallengeConsent(childId: string, enabled: boolean): Promise<void> {
  await apiClient.put(`/progress/challenges/consent/${childId}`, { enabled })
}
