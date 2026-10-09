import type { ReactElement } from 'react'
import { useApproveChildWord, usePendingApprovals } from '../../hooks/useAutofill'
import { SenseCard } from '../autofill/SenseCard'
import { successButtonClass } from './supportUi'

type WordsToApproveProps = Readonly<{
  learnerId: string
  learnerName: string
}>

/**
 * "Words to approve" for one supported learner: auto-filled words the child added. Approve makes the
 * word visible to this child only. Rendered only for supporters (never on a child account).
 */
export function WordsToApprove({ learnerId, learnerName }: WordsToApproveProps): ReactElement | null {
  const { data: words, isLoading, isError } = usePendingApprovals(learnerId)
  const approve = useApproveChildWord(learnerId)

  if (isLoading) return null
  if (isError) {
    return <p className="mt-2 text-sm text-wb-ink-muted">Couldn't load words to approve right now.</p>
  }
  if (!words || words.length === 0) return null

  return (
    <section className="mt-3 flex flex-col gap-3" aria-label={`Words to approve for ${learnerName}`}>
      <h3 className="text-lg font-bold text-wb-ink">Words to approve</h3>
      <div className="grid gap-3 sm:grid-cols-2">
        {words.map((word, index) => (
          <SenseCard key={word.senseId} sense={word} index={index}>
            <button
              type="button"
              disabled={approve.isPending}
              onClick={() => approve.mutate(word.senseId)}
              className={successButtonClass}
            >
              Approve
            </button>
            {word.childSuitableHint === false && (
              <span className="self-center text-xs font-semibold text-wb-danger">May not suit children</span>
            )}
          </SenseCard>
        ))}
      </div>
      {approve.isError && <p className="text-sm text-wb-danger">Couldn't approve this word. Please try again.</p>}
    </section>
  )
}
