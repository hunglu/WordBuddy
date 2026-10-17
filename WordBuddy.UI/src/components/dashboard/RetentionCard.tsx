import type { ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import type { DashboardRetention } from '../../types'
import { cardClass } from '../support/supportUi'

type RetentionCardProps = Readonly<{ retention: DashboardRetention }>

/** Hero number: true retention (first attempt of due words), overall and per skill. */
export function RetentionCard({ retention }: RetentionCardProps): ReactElement {
  const { t } = useTranslation(['dashboard', 'vocabulary'])
  return (
    <section className={cardClass} aria-label={t('dashboard:retention.title')}>
      <h2 className="text-lg font-bold text-wb-ink">{t('dashboard:retention.title')}</h2>
      {retention.overall === null ? (
        <>
          <p className="mt-2 text-3xl font-extrabold text-wb-ink-muted">{t('dashboard:retention.notEnough')}</p>
          <p className="mt-1 text-sm text-wb-ink-muted">{t('dashboard:retention.notEnoughHint', { sample: retention.sample })}</p>
        </>
      ) : (
        <>
          <p className="mt-2 text-6xl font-extrabold text-wb-success">{retention.overall}%</p>
          <p className="mt-1 text-sm text-wb-ink-muted">
            {t('dashboard:retention.firstTry', { sample: retention.sample })}
          </p>
        </>
      )}

      {retention.bySkill.length > 0 && (
        <ul className="mt-4 flex flex-wrap gap-2">
          {retention.bySkill.map((skill) => (
            <li key={skill.skill} className="rounded-wb-pill bg-wb-hover-tint px-3 py-1 text-sm font-semibold text-wb-ink">
              {t(`vocabulary:skill.${skill.skill}`)}: {skill.retention === null ? t('dashboard:retention.noData') : `${skill.retention}%`}
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
