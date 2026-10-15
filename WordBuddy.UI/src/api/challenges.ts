import type { Challenge, ChallengeBoard, PageParams, PagedResult } from '../types'
import { apiClient } from './client'

/** Problem code: the challenge has no room left. */
export const CHALLENGE_FULL_CODE = 'Challenge.Full'

/** Problem code: a child needs Guardian consent first. */
export const CHALLENGE_NO_CONSENT_CODE = 'Challenge.NoConsent'

/** One page of the open and running challenges of the caller audience. A child without consent gets an empty page. */
export async function listChallenges(paging: PageParams): Promise<PagedResult<Challenge>> {
  const { data } = await apiClient.get<PagedResult<Challenge>>('/progress/challenges', { params: paging })
  return data
}

/**
 * One page of the board of one challenge. The server filters it for the caller (adult: paged full
 * ranking, child: own rank + top 5 on a single page, paging ignored).
 */
export async function getChallengeBoard(id: string, paging: PageParams): Promise<ChallengeBoard> {
  const { data } = await apiClient.get<ChallengeBoard>(`/progress/challenges/${id}/board`, { params: paging })
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
