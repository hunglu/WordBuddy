import type { ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import type { WeekActivity } from '../../types'
import { CountUpStat } from '../CountUpStat'
import { cardClass } from '../support/supportUi'

type StreakCardProps = Readonly<{ streakDays: number; weeks: WeekActivity[] }>

/** Current streak and active days per week. */
export function StreakCard({ streakDays, weeks }: StreakCardProps): ReactElement {
  const { t } = useTranslation('dashboard')
  return (
    <section className={cardClass} aria-label={t('streak.title')}>
      <h2 className="text-lg font-bold text-wb-ink">{t('streak.title')}</h2>
      <p className="mt-2 text-5xl font-extrabold text-wb-highlight">
        🔥 <CountUpStat value={streakDays} />
      </p>
      <p className="mt-1 text-sm text-wb-ink-muted">{t('streak.day', { count: streakDays })}</p>

      {weeks.length > 0 && (
        <ul className="mt-4 flex flex-col gap-1 text-sm text-wb-ink">
          {weeks.map((week) => (
            <li key={week.weekStart} className="flex justify-between gap-3">
              <span className="text-wb-ink-muted">{t('streak.weekOf', { date: week.weekStart })}</span>
              <span className="font-semibold">{t('streak.activeDays', { count: week.activeDays })}</span>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
