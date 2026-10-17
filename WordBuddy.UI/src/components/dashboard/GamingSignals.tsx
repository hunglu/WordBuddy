import type { ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import type { DashboardGamingSignals } from '../../types'
import { cardClass } from '../support/supportUi'

type GamingSignalsProps = Readonly<{ gaming: DashboardGamingSignals }>

/** Answer-pattern facts. Neutral wording on purpose: these are hints to talk about, not a verdict. */
export function GamingSignals({ gaming }: GamingSignalsProps): ReactElement {
  const { t } = useTranslation('dashboard')
  return (
    <section className={cardClass} aria-label={t('gaming.title')}>
      <h2 className="text-lg font-bold text-wb-ink">{t('gaming.title')}</h2>
      {gaming.totalAnswers === 0 ? (
        <p className="mt-2 text-wb-ink-muted">{t('gaming.none')}</p>
      ) : (
        <dl className="mt-3 flex flex-col gap-2 text-sm text-wb-ink">
          <div className="flex justify-between gap-3">
            <dt>{t('gaming.quickWrong')}</dt>
            <dd className="font-semibold">
              {gaming.quickWrongCount} ({gaming.quickWrongPercent}%)
            </dd>
          </div>
          <div className="flex justify-between gap-3">
            <dt>{t('gaming.hint')}</dt>
            <dd className="font-semibold">
              {gaming.hintPercent}%{gaming.hintRateFlagged ? t('gaming.higher') : ''}
            </dd>
          </div>
          <div className="flex justify-between gap-3">
            <dt>{t('gaming.unfinished')}</dt>
            <dd className="font-semibold">{gaming.unfinishedSessions}</dd>
          </div>
        </dl>
      )}
    </section>
  )
}
