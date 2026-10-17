import type { ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import { shareStatusClasses } from '../../theme/variants'
import type { AdminLexemeSense } from '../../types'
import { cardClass } from '../support/supportUi'

type LexemeSensesPanelProps = Readonly<{
  /** Catalog senses only (lesson words and shared-pool words). */
  senses: readonly AdminLexemeSense[]
  /** Private, pending and rejected learner senses: only the count is known. */
  privateLearnerSenseCount: number
}>

/** Read-only list of the meanings of a lexeme. Private learner text is never shown. */
export function LexemeSensesPanel({ senses, privateLearnerSenseCount }: LexemeSensesPanelProps): ReactElement {
  const { t } = useTranslation('admin')
  return (
    <section className="mt-6">
      <h2 className="text-xl font-bold text-wb-ink">{t('lexemeSenses.heading', { count: senses.length })}</h2>
      {senses.length === 0 && <p className="mt-2 text-wb-ink-muted">{t('lexemeSenses.none')}</p>}

      <ul className="mt-3 flex flex-col gap-2">
        {senses.map((sense) => (
          <li key={sense.id} className={cardClass}>
            <div className="flex flex-wrap items-center gap-2">
              <span className="font-bold text-wb-ink">{sense.word}</span>
              <span className="rounded-wb-pill border border-wb-border-subtle px-2 py-0.5 text-xs font-semibold text-wb-ink">
                {t(`lexemeSenses.source.${sense.source}`)}
              </span>
              <span className={`rounded-wb-pill border px-2 py-0.5 text-xs font-semibold ${shareStatusClasses[sense.shareStatus]}`}>
                {t(`lexemeSenses.share.${sense.shareStatus}`)}
              </span>
              <span className="rounded-wb-pill border border-wb-border-subtle px-2 py-0.5 text-xs font-semibold text-wb-ink">
                {sense.origin === 'AutoFill' ? t('lexemeSenses.autoFill') : t('lexemeSenses.manual')}
              </span>
              <span className="rounded-wb-pill border border-wb-border-subtle px-2 py-0.5 text-xs font-semibold text-wb-ink">
                {sense.visibleToChildren ? t('lexemeSenses.visible') : t('lexemeSenses.hidden')}
              </span>
            </div>
            <p className="mt-2 text-wb-ink">{sense.definition}</p>
            {sense.example && <p className="mt-1 text-sm italic text-wb-ink-muted">{sense.example}</p>}
          </li>
        ))}
      </ul>

      <p className="mt-3 text-sm text-wb-ink-muted">
        {t('lexemeSenses.privateCount', { count: privateLearnerSenseCount })}
      </p>
    </section>
  )
}
