import type { TFunction } from 'i18next'
import { currentLocale } from '../../i18n'
import type { Challenge } from '../../types'

/** One short line that explains the goal of a challenge. */
export function goalText(t: TFunction<'challenges'>, challenge: Pick<Challenge, 'kind' | 'metric' | 'target'>): string {
  const metric: string = t(`metric.${challenge.metric}`)
  return challenge.kind === 'Target' && challenge.target !== null
    ? t('goalTarget', { metric, target: challenge.target })
    : t('goalRanked', { metric })
}

/** Date range for display in the current UI language, for example `10/11/2026 to 10/18/2026`. */
export function periodText(t: TFunction<'challenges'>, startsAtUtc: string, endsAtUtc: string): string {
  const locale: string = currentLocale()
  const start: string = new Date(startsAtUtc).toLocaleDateString(locale)
  // The end is exclusive: show the last day that counts.
  const end: string = new Date(new Date(endsAtUtc).getTime() - 1).toLocaleDateString(locale)
  return t('period', { start, end })
}
