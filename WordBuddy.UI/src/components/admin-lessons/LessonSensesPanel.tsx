import { AnimatePresence, motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import { problemMessage } from '../../api/supportLinks'
import { useDetachLessonSense, useReorderLessonSenses } from '../../hooks/useAdminLessons'
import type { AdminLessonSenseDto } from '../../types'
import { dangerButtonClass, softButtonClass } from '../support/supportUi'
import { AddWordPicker } from './AddWordPicker'

type LessonSensesPanelProps = Readonly<{
  lessonId: string
  /** The attached words in lesson order. */
  senses: readonly AdminLessonSenseDto[]
}>

/** Returns the sense ids with the item at `index` moved by `delta` places. */
function movedIds(senses: readonly AdminLessonSenseDto[], index: number, delta: -1 | 1): string[] {
  const ids: string[] = senses.map((s) => s.senseId)
  const target: number = index + delta
  ;[ids[index], ids[target]] = [ids[target], ids[index]]
  return ids
}

/** Word list of a vocabulary lesson: reorder with up/down, remove, and add from the picker. */
export function LessonSensesPanel({ lessonId, senses }: LessonSensesPanelProps): ReactElement {
  const { t } = useTranslation('admin')
  const reorder = useReorderLessonSenses()
  const detach = useDetachLessonSense()
  const busy: boolean = reorder.isPending || detach.isPending
  const attachedIds: ReadonlySet<string> = new Set(senses.map((s) => s.senseId))

  return (
    <section className="mt-8 grid gap-6 lg:grid-cols-2">
      <div>
        <h2 className="text-2xl font-bold text-wb-ink">{t('senses.heading', { count: senses.length })}</h2>
        {senses.length === 0 && <p className="mt-3 text-wb-ink-muted">{t('senses.none')}</p>}
        {(reorder.isError || detach.isError) && (
          <p role="alert" className="mt-3 text-sm font-semibold text-wb-danger">
            {problemMessage(reorder.error ?? detach.error, t('senses.changeFailed'))}
          </p>
        )}

        <ol className="mt-3 flex flex-col gap-2">
          <AnimatePresence initial={false}>
            {senses.map((sense, index) => (
              <motion.li
                key={sense.senseId}
                layout
                initial={{ opacity: 0, y: 8 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0 }}
                transition={{ duration: 0.2 }}
                className="flex items-center gap-3 rounded-wb-card bg-wb-surface-card p-3 shadow-wb-card"
              >
                <span className="w-6 text-center text-sm font-bold text-wb-ink-muted">{index + 1}</span>
                <div className="min-w-0 flex-1">
                  <p className="font-bold text-wb-ink">
                    {sense.word}
                    {!sense.visibleToChildren && (
                      <span className="ml-2 rounded-wb-pill bg-wb-danger-soft px-2 py-0.5 text-xs font-semibold text-wb-danger-soft-ink">
                        {t('senses.hidden')}
                      </span>
                    )}
                  </p>
                  <p className="truncate text-sm text-wb-ink-muted">{sense.definition}</p>
                </div>
                <div className="flex gap-1">
                  <button
                    type="button"
                    aria-label={t('senses.moveUp', { word: sense.word })}
                    disabled={busy || index === 0}
                    onClick={() => reorder.mutate({ lessonId, senseIds: movedIds(senses, index, -1) })}
                    className={softButtonClass}
                  >
                    ↑
                  </button>
                  <button
                    type="button"
                    aria-label={t('senses.moveDown', { word: sense.word })}
                    disabled={busy || index === senses.length - 1}
                    onClick={() => reorder.mutate({ lessonId, senseIds: movedIds(senses, index, 1) })}
                    className={softButtonClass}
                  >
                    ↓
                  </button>
                  <button
                    type="button"
                    aria-label={t('senses.removeWord', { word: sense.word })}
                    disabled={busy}
                    onClick={() => detach.mutate({ lessonId, senseId: sense.senseId })}
                    className={dangerButtonClass}
                  >
                    {t('senses.remove')}
                  </button>
                </div>
              </motion.li>
            ))}
          </AnimatePresence>
        </ol>
      </div>

      <AddWordPicker lessonId={lessonId} attachedSenseIds={attachedIds} />
    </section>
  )
}
