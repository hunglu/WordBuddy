import type { ReactElement } from 'react'
import { useAdminPendingApprovals, useApproveAutofillForChildren } from '../../hooks/useAutofill'
import { SenseCard } from './SenseCard'

/**
 * Admin "Auto-filled words" tab: auto-filled words that children added and wait on. Approve makes
 * the word visible to every child. The page itself is admin-only (`AdminOnly` on the API).
 */
export function AutofillApprovalsPanel(): ReactElement {
  const { data: words, isLoading, isError } = useAdminPendingApprovals()
  const approve = useApproveAutofillForChildren()

  if (isLoading) return <p className="mt-8 text-lg text-wb-ink-muted">Loading auto-filled words…</p>
  if (isError) return <p className="mt-8 text-lg text-wb-danger">Couldn't load auto-filled words right now.</p>
  if (!words || words.length === 0) {
    return <p className="mt-8 text-lg text-wb-ink-muted">No auto-filled words wait for child approval.</p>
  }

  return (
    <div className="mt-6 flex flex-col gap-4">
      <p className="text-wb-ink-muted">Approving shows the word to every child.</p>
      <div className="grid gap-4 sm:grid-cols-2">
        {words.map((word, index) => (
          <SenseCard key={word.senseId} sense={word} index={index}>
            <button
              type="button"
              disabled={approve.isPending}
              onClick={() => approve.mutate(word.senseId)}
              className="rounded-wb-md bg-wb-success px-4 py-2 text-sm font-bold text-wb-on-success shadow-wb-card hover:bg-wb-success-hover disabled:opacity-60"
            >
              Approve for children
            </button>
            {word.childSuitableHint === false && (
              <span className="self-center text-xs font-semibold text-wb-danger">Flagged: may not suit children</span>
            )}
          </SenseCard>
        ))}
      </div>
      {approve.isError && <p className="text-sm text-wb-danger">Couldn't approve this word. Please try again.</p>}
    </div>
  )
}
