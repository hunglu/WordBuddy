import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import type { WordsAdded } from '../../types'
import { cardClass } from '../support/supportUi'

type WordsAddedChartProps = Readonly<{ added: WordsAdded[]; unit: 'day' | 'week' }>

const MAX_BAR_PX = 96

function supporterTotal(bucket: WordsAdded): number {
  return bucket.supporters.reduce((sum, supporter) => sum + supporter.count, 0)
}

/** Words added per day or week: the learner's own against supporters'. Plain bars, no chart library. */
export function WordsAddedChart({ added, unit }: WordsAddedChartProps): ReactElement {
  const max = added.reduce((highest, bucket) => Math.max(highest, bucket.self + supporterTotal(bucket)), 0)
  const barPx = (count: number): number => (max === 0 ? 0 : Math.round((count / max) * MAX_BAR_PX))

  return (
    <section className={cardClass} aria-label="Words added">
      <h2 className="text-lg font-bold text-wb-ink">Words added per {unit}</h2>
      {added.length === 0 ? (
        <p className="mt-2 text-wb-ink-muted">No words added in this period.</p>
      ) : (
        <>
          <ul className="mt-4 flex items-end gap-2 overflow-x-auto pb-1">
            {added.map((bucket) => (
              <li key={bucket.date} className="flex flex-col items-center gap-1" title={`${bucket.date}`}>
                <div className="flex w-6 flex-col justify-end">
                  <motion.div
                    className="w-full rounded-t-sm bg-wb-secondary"
                    initial={{ height: 0 }}
                    animate={{ height: barPx(supporterTotal(bucket)) }}
                  />
                  <motion.div
                    className="w-full bg-wb-primary"
                    initial={{ height: 0 }}
                    animate={{ height: barPx(bucket.self) }}
                  />
                </div>
                <span className="text-xs text-wb-ink-muted">{bucket.date.slice(5)}</span>
              </li>
            ))}
          </ul>
          <p className="mt-2 flex gap-4 text-sm text-wb-ink-muted">
            <span>
              <span className="mr-1 inline-block h-3 w-3 rounded-sm bg-wb-primary align-middle" />
              Added by the learner
            </span>
            <span>
              <span className="mr-1 inline-block h-3 w-3 rounded-sm bg-wb-secondary align-middle" />
              Added by supporters
            </span>
          </p>
        </>
      )}
    </section>
  )
}
