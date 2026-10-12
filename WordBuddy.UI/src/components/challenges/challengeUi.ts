import type { Challenge, ChallengeMetric, ChallengeStatus } from '../../types'

export const metricLabel: Record<ChallengeMetric, string> = {
  MemoryPoints: 'Memory points',
  DueReviewsCorrect: 'Days with a correct review',
  RecallUpgrades: 'Words moved to recall',
  ListeningCorrect: 'Correct listening reviews',
}

export const statusLabel: Record<ChallengeStatus, string> = {
  Scheduled: 'Soon',
  Open: 'Open to join',
  Running: 'Running',
  Finished: 'Finished',
}

/** One short line that explains the goal of a challenge. */
export function goalText(challenge: Pick<Challenge, 'kind' | 'metric' | 'target'>): string {
  const metric = metricLabel[challenge.metric]
  return challenge.kind === 'Target' && challenge.target !== null
    ? `${metric}: reach ${challenge.target}`
    : `${metric}: most wins`
}

/** Date range for display, for example `10/11/2026 to 10/18/2026`. */
export function periodText(startsAtUtc: string, endsAtUtc: string): string {
  const start = new Date(startsAtUtc).toLocaleDateString()
  // The end is exclusive: show the last day that counts.
  const end = new Date(new Date(endsAtUtc).getTime() - 1).toLocaleDateString()
  return `${start} to ${end}`
}
