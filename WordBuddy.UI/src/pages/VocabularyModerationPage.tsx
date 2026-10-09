import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useState } from 'react'
import { Navigate, useSearchParams } from 'react-router-dom'
import { AutofillApprovalsPanel } from '../components/autofill/AutofillApprovalsPanel'
import { usePendingVocabularyModeration, useModerateVocabularyWord } from '../hooks/useVocabulary'
import { useAuthStore } from '../store/authStore'

export function VocabularyModerationPage(): ReactElement {
  const user = useAuthStore((state) => state.user)
  const [searchParams, setSearchParams] = useSearchParams()
  const tab: 'shared' | 'autofill' = searchParams.get('tab') === 'autofill' ? 'autofill' : 'shared'

  // Server-side, `AdminOnly` already rejects a non-admin call — this is a UX guard, not the
  // real enforcement.
  if (!user?.isAdmin) {
    return <Navigate to="/vocabulary" replace />
  }

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <h1 className="text-3xl font-extrabold text-wb-ink">Vocabulary Moderation</h1>

      <div role="tablist" className="mt-4 flex gap-2">
        {TABS.map(([key, label]) => (
          <button
            key={key}
            type="button"
            role="tab"
            aria-selected={tab === key}
            onClick={() => setSearchParams(key === 'shared' ? {} : { tab: key })}
            className={`rounded-wb-md px-4 py-2 text-sm font-bold ${
              tab === key ? 'bg-wb-primary text-wb-on-primary' : 'bg-wb-primary-soft text-wb-ink hover:bg-wb-primary-soft-hover'
            }`}
          >
            {label}
          </button>
        ))}
      </div>

      {tab === 'autofill' ? <AutofillApprovalsPanel /> : <SharedWordsQueue />}
    </motion.div>
  )
}

/** Tabs of the moderation page (URL `?tab=`). */
const TABS = [
  ['shared', 'Shared words'],
  ['autofill', 'Auto-filled words'],
] as const

/** The original moderation queue: learner words asking to join the shared pool. */
function SharedWordsQueue(): ReactElement {
  const { data: words, isLoading, isError } = usePendingVocabularyModeration()
  const moderate = useModerateVocabularyWord()
  const [visibleToChildrenByWord, setVisibleToChildrenByWord] = useState<Record<string, boolean>>({})

  return (
    <>
      <p className="mt-2 text-wb-ink-muted">Words awaiting approval before they join the shared pool.</p>

      {isLoading && <p className="mt-8 text-lg text-wb-ink-muted">Loading the moderation queue…</p>}
      {isError && <p className="mt-8 text-lg text-wb-danger">Couldn't load the moderation queue right now.</p>}

      {words && words.length === 0 && (
        <p className="mt-8 text-lg text-wb-ink-muted">Nothing pending review right now.</p>
      )}

      {words && words.length > 0 && (
        <div className="mt-6 flex flex-col gap-4">
          {words.map((word) => {
            const visibleToChildren = visibleToChildrenByWord[word.id] ?? false

            return (
              <div key={word.id} className="rounded-wb-card bg-wb-surface-card p-5 shadow-wb-card">
                <p className="text-xl font-bold text-wb-ink">{word.word}</p>
                <p className="text-wb-ink-muted">{word.definition}</p>
                {word.example && <p className="text-sm italic text-wb-ink-muted">"{word.example}"</p>}

                <label className="mt-3 flex items-center gap-2 text-sm font-semibold text-wb-ink">
                  <input
                    type="checkbox"
                    checked={visibleToChildren}
                    onChange={(e) =>
                      setVisibleToChildrenByWord((prev) => ({ ...prev, [word.id]: e.target.checked }))
                    }
                  />
                  Visible to children
                </label>

                <div className="mt-3 flex gap-2">
                  <button
                    type="button"
                    onClick={() => moderate.mutate({ id: word.id, payload: { approve: true, visibleToChildren } })}
                    disabled={moderate.isPending}
                    className="rounded-wb-md bg-wb-success px-4 py-2 text-sm font-bold text-wb-on-success hover:bg-wb-success-hover shadow-wb-card disabled:opacity-60"
                  >
                    Approve
                  </button>
                  <button
                    type="button"
                    onClick={() => moderate.mutate({ id: word.id, payload: { approve: false, visibleToChildren: false } })}
                    disabled={moderate.isPending}
                    className="rounded-wb-md bg-wb-danger-soft px-4 py-2 text-sm font-bold text-wb-danger-soft-ink hover:bg-wb-danger-soft-hover disabled:opacity-60"
                  >
                    Reject
                  </button>
                </div>
              </div>
            )
          })}
        </div>
      )}
    </>
  )
}
