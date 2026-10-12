import type { ReactElement } from 'react'
import { Link } from 'react-router-dom'
import { CHALLENGE_FULL_CODE, CHALLENGE_NO_CONSENT_CODE } from '../../api/challenges'
import { problemCode, problemMessage } from '../../api/supportLinks'
import { useJoinChallenge, useLeaveChallenge } from '../../hooks/useChallenges'
import type { Challenge } from '../../types'
import { cardClass, dangerButtonClass, primaryButtonClass, softButtonClass } from '../support/supportUi'
import { goalText, periodText, statusLabel } from './challengeUi'

type ChallengeCardProps = Readonly<{ challenge: Challenge }>

/** Maps a join/leave error to a short message for the learner. */
function actionMessage(error: unknown): string {
  const code = problemCode(error)
  if (code === CHALLENGE_FULL_CODE) {
    return 'This challenge is full'
  }
  if (code === CHALLENGE_NO_CONSENT_CODE) {
    return 'Ask your guardian to allow challenges.'
  }
  return problemMessage(error, 'That did not work. Please try again.')
}

/** One challenge with its join/leave action and a link to the board. */
export function ChallengeCard({ challenge }: ChallengeCardProps): ReactElement {
  const join = useJoinChallenge()
  const leave = useLeaveChallenge()
  const busy = join.isPending || leave.isPending
  const error = join.error ?? leave.error
  const full = challenge.participantCount >= challenge.capacity

  return (
    <section aria-label={challenge.name} className={cardClass}>
      <div className="flex flex-wrap items-center gap-2">
        <p className="text-xl font-bold text-wb-ink">{challenge.name}</p>
        <span className="rounded-wb-md bg-wb-primary-soft px-2 py-1 text-xs font-bold text-wb-ink">
          {statusLabel[challenge.status]}
        </span>
        {challenge.joined && (
          <span className="rounded-wb-md bg-wb-hover-tint px-2 py-1 text-xs font-semibold text-wb-ink-muted">Joined</span>
        )}
      </div>
      <p className="mt-1 text-wb-ink">{goalText(challenge)}</p>
      <p className="mt-1 text-sm text-wb-ink-muted">
        {periodText(challenge.startsAtUtc, challenge.endsAtUtc)} · {challenge.participantCount} of {challenge.capacity} learners
      </p>
      {challenge.joined && challenge.myScore !== null && (
        <p className="mt-1 text-sm font-semibold text-wb-ink">
          Your score: {challenge.myScore}
          {challenge.myRank !== null ? ` · rank ${challenge.myRank}` : ''}
        </p>
      )}

      <div className="mt-3 flex flex-wrap items-center gap-2">
        {challenge.joined ? (
          <>
            <Link to={`/challenges/${challenge.id}`} className={primaryButtonClass}>
              View board
            </Link>
            <button type="button" disabled={busy} onClick={() => leave.mutate(challenge.id)} className={dangerButtonClass}>
              Leave
            </button>
          </>
        ) : (
          <button
            type="button"
            disabled={busy || full}
            onClick={() => join.mutate(challenge.id)}
            className={softButtonClass}
          >
            {full ? 'Full' : 'Join'}
          </button>
        )}
      </div>

      {full && !challenge.joined && <p className="mt-2 text-sm text-wb-ink-muted">This challenge is full</p>}
      {error && <p className="mt-2 text-sm text-wb-danger">{actionMessage(error)}</p>}
    </section>
  )
}
