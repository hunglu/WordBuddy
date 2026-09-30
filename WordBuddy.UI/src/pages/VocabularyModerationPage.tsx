import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useState } from 'react'
import { Navigate } from 'react-router-dom'
import { usePendingVocabularyModeration, useModerateVocabularyWord } from '../hooks/useVocabulary'
import { useAuthStore } from '../store/authStore'

export function VocabularyModerationPage(): ReactElement {
  const user = useAuthStore((state) => state.user)

  const { data: words, isLoading, isError } = usePendingVocabularyModeration()
  const moderate = useModerateVocabularyWord()
  const [visibleToChildrenByWord, setVisibleToChildrenByWord] = useState<Record<string, boolean>>({})

  // Server-side, `AdminOnly` already rejects a non-admin call — this is a UX guard, not the
  // real enforcement.
  if (!user?.isAdmin) {
    return <Navigate to="/vocabulary" replace />
  }

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <h1 className="text-3xl font-extrabold text-wb-ink">Vocabulary Moderation</h1>
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
                    className="rounded-wb-md bg-wb-success px-4 py-2 text-sm font-bold text-wb-on-success shadow disabled:opacity-60"
                  >
                    Approve
                  </button>
                  <button
                    type="button"
                    onClick={() => moderate.mutate({ id: word.id, payload: { approve: false, visibleToChildren: false } })}
                    disabled={moderate.isPending}
                    className="rounded-wb-md bg-wb-danger-soft px-4 py-2 text-sm font-bold text-wb-danger hover:bg-rose-200 disabled:opacity-60"
                  >
                    Reject
                  </button>
                </div>
              </div>
            )
          })}
        </div>
      )}
    </motion.div>
  )
}
