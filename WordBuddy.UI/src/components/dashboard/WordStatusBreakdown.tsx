import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import type { WordStatusCount } from '../../types'
import { cardClass } from '../support/supportUi'

type WordStatusBreakdownProps = Readonly<{ perStatus: WordStatusCount[] }>

/** Active words per status as horizontal bars. */
export function WordStatusBreakdown({ perStatus }: WordStatusBreakdownProps): ReactElement {
  const { t } = useTranslation(['dashboard', 'vocabulary'])
  const total = perStatus.reduce((sum, item) => sum + item.count, 0)

  return (
    <section className={cardClass} aria-label={t('dashboard:status.title')}>
      <h2 className="text-lg font-bold text-wb-ink">{t('dashboard:status.title')}</h2>
      {total === 0 ? (
        <p className="mt-2 text-wb-ink-muted">{t('dashboard:status.none')}</p>
      ) : (
        <ul className="mt-3 flex flex-col gap-2">
          {perStatus.map((item) => (
            <li key={item.status}>
              <div className="flex justify-between text-sm text-wb-ink">
                <span>{t(`vocabulary:wordStatus.${item.status}`)}</span>
                <span className="font-semibold">{item.count}</span>
              </div>
              <div className="mt-1 h-2 overflow-hidden rounded-wb-pill bg-wb-hover-tint">
                <motion.div
                  className="h-full rounded-wb-pill bg-wb-primary"
                  initial={{ width: 0 }}
                  animate={{ width: `${Math.round((item.count / total) * 100)}%` }}
                  transition={{ duration: 0.4 }}
                />
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
