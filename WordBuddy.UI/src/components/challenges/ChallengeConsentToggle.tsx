import type { ReactElement } from 'react'
import { problemMessage } from '../../api/supportLinks'
import { useSetChildChallengeConsent } from '../../hooks/useChallenges'
import { cardClass, dangerButtonClass, successButtonClass } from '../support/supportUi'

type ChallengeConsentToggleProps = Readonly<{
  childId: string
  childName: string
}>

/**
 * "Allow challenges" for one child. Shown only to a Guardian (JWT `guardian` claim). The server checks the Guardian
 * role and the active link again. There is no read endpoint for the current state, so both actions are offered.
 */
export function ChallengeConsentToggle({ childId, childName }: ChallengeConsentToggleProps): ReactElement {
  const consent = useSetChildChallengeConsent()
  const lastEnabled = consent.variables?.enabled

  return (
    <div className={`mt-2 ${cardClass}`}>
      <p className="text-lg font-bold text-wb-ink">Challenges for {childName}</p>
      <p className="text-sm text-wb-ink-muted">
        Allow {childName} to join vocabulary challenges with other children. Children see only an alias and an avatar.
      </p>
      <div className="mt-3 flex flex-wrap gap-2">
        <button
          type="button"
          disabled={consent.isPending}
          onClick={() => consent.mutate({ childId, enabled: true })}
          className={successButtonClass}
        >
          Allow challenges
        </button>
        <button
          type="button"
          disabled={consent.isPending}
          onClick={() => consent.mutate({ childId, enabled: false })}
          className={dangerButtonClass}
        >
          Stop challenges
        </button>
      </div>
      {consent.isSuccess && (
        <p className="mt-2 text-sm text-wb-ink">{lastEnabled === true ? 'Challenges are allowed.' : 'Challenges are stopped.'}</p>
      )}
      {consent.isError && (
        <p className="mt-2 text-sm text-wb-danger">{problemMessage(consent.error, 'That did not work. Please try again.')}</p>
      )}
    </div>
  )
}
