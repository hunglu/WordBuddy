import type { ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import type { TFunction } from 'i18next'
import { Link } from 'react-router-dom'
import { CHALLENGE_FULL_CODE, CHALLENGE_NO_CONSENT_CODE } from '../../api/challenges'
import { problemCode, problemMessage } from '../../api/supportLinks'
import { useJoinChallenge, useLeaveChallenge } from '../../hooks/useChallenges'
import type { Challenge } from '../../types'
import { cardClass, dangerButtonClass, primaryButtonClass, softButtonClass } from '../support/supportUi'
import { goalText, periodText } from './challengeUi'

type ChallengeCardProps = Readonly<{ challenge: Challenge }>

/** Maps a join/leave error to a short message for the learner. */
function actionMessage(t: TFunction<'challenges'>, error: unknown): string {
  const code = problemCode(error)
  if (code === CHALLENGE_FULL_CODE) {
    return t('card.fullNote')
  }
  if (code === CHALLENGE_NO_CONSENT_CODE) {
    return t('card.noConsent')
  }
  return problemMessage(error, t('card.failed'))
}

/** One challenge with its join/leave action and a link to the board. */
export function ChallengeCard({ challenge }: ChallengeCardProps): ReactElement {
  const { t } = useTranslation('challenges')
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
          {t(`status.${challenge.status}`)}
        </span>
        {challenge.joined && (
          <span className="rounded-wb-md bg-wb-hover-tint px-2 py-1 text-xs font-semibold text-wb-ink-muted">{t('card.joined')}</span>
        )}
      </div>
      <p className="mt-1 text-wb-ink">{goalText(t, challenge)}</p>
      <p className="mt-1 text-sm text-wb-ink-muted">
        {periodText(t, challenge.startsAtUtc, challenge.endsAtUtc)} · {t('card.learners', { count: challenge.participantCount, capacity: challenge.capacity })}
      </p>
      {challenge.joined && challenge.myScore !== null && (
        <p className="mt-1 text-sm font-semibold text-wb-ink">
          {challenge.myRank !== null
            ? t('card.scoreRank', { score: challenge.myScore, rank: challenge.myRank })
            : t('card.score', { score: challenge.myScore })}
        </p>
      )}

      <div className="mt-3 flex flex-wrap items-center gap-2">
        {challenge.joined ? (
          <>
            <Link to={`/challenges/${challenge.id}`} className={primaryButtonClass}>
              {t('card.viewBoard')}
            </Link>
            <button type="button" disabled={busy} onClick={() => leave.mutate(challenge.id)} className={dangerButtonClass}>
              {t('card.leave')}
            </button>
          </>
        ) : (
          <button
            type="button"
            disabled={busy || full}
            onClick={() => join.mutate(challenge.id)}
            className={softButtonClass}
          >
            {full ? t('card.full') : t('card.join')}
          </button>
        )}
      </div>

      {full && !challenge.joined && <p className="mt-2 text-sm text-wb-ink-muted">{t('card.fullNote')}</p>}
      {error && <p className="mt-2 text-sm text-wb-danger">{actionMessage(t, error)}</p>}
    </section>
  )
}
