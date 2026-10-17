import type { ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import { useSenses } from '../../hooks/useVocabularyReview'
import type { DashboardStruggle } from '../../types'
import { cardClass } from '../support/supportUi'

type StruggleListProps = Readonly<{ struggle: DashboardStruggle }>

/** Words that need attention. Progress returns sense ids only; text comes from Content. */
export function StruggleList({ struggle }: StruggleListProps): ReactElement {
  const { t } = useTranslation(['dashboard', 'vocabulary'])
  const ids = [...new Set([...struggle.leeches.map((word) => word.senseId), ...struggle.slowestWords.map((word) => word.senseId)])]
  // If Content fails or hides a word, the list still renders with a placeholder.
  const senses = useSenses(ids)
  const wordOf = (senseId: string): string =>
    senses.data?.find((sense) => sense.senseId === senseId)?.word ?? t('dashboard:struggle.unknownWord')

  return (
    <section className={cardClass} aria-label={t('dashboard:struggle.title')}>
      <h2 className="text-lg font-bold text-wb-ink">{t('dashboard:struggle.title')}</h2>

      <p className="mt-2 text-sm text-wb-ink">
        {t('dashboard:struggle.weakest')}{' '}
        <span className="font-semibold">
          {struggle.weakestSkill === null ? t('dashboard:struggle.noData') : t(`vocabulary:skill.${struggle.weakestSkill}`)}
        </span>
      </p>

      <h3 className="mt-4 font-semibold text-wb-ink">{t('dashboard:struggle.leeches')}</h3>
      {struggle.leeches.length === 0 ? (
        <p className="text-sm text-wb-ink-muted">{t('dashboard:struggle.none')}</p>
      ) : (
        <ul className="mt-1 flex flex-col gap-1 text-sm text-wb-ink">
          {struggle.leeches.map((word) => (
            <li key={word.senseId} className="flex justify-between gap-3">
              <span>{wordOf(word.senseId)}</span>
              <span className="text-wb-ink-muted">{t('dashboard:struggle.forgotten', { count: word.lapses })}</span>
            </li>
          ))}
        </ul>
      )}

      <h3 className="mt-4 font-semibold text-wb-ink">{t('dashboard:struggle.slowest')}</h3>
      {struggle.slowestWords.length === 0 ? (
        <p className="text-sm text-wb-ink-muted">{t('dashboard:struggle.notEnough')}</p>
      ) : (
        <ul className="mt-1 flex flex-col gap-1 text-sm text-wb-ink">
          {struggle.slowestWords.map((word) => (
            <li key={word.senseId} className="flex justify-between gap-3">
              <span>{wordOf(word.senseId)}</span>
              <span className="text-wb-ink-muted">{t('dashboard:struggle.about', { seconds: (word.medianResponseMs / 1000).toFixed(1) })}</span>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
