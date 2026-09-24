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
      <h1 className="text-3xl font-extrabold text-sky-900">Vocabulary Moderation</h1>
      <p className="mt-2 text-sky-700">Words awaiting approval before they join the shared pool.</p>

      {isLoading && <p className="mt-8 text-lg text-sky-600">Loading the moderation queue…</p>}
      {isError && <p className="mt-8 text-lg text-rose-600">Couldn't load the moderation queue right now.</p>}

      {words && words.length === 0 && (
        <p className="mt-8 text-lg text-sky-600">Nothing pending review right now.</p>
      )}

      {words && words.length > 0 && (
        <div className="mt-6 flex flex-col gap-4">
          {words.map((word) => {
            const visibleToChildren = visibleToChildrenByWord[word.id] ?? false

            return (
              <div key={word.id} className="rounded-3xl bg-white p-5 shadow-sm">
                <p className="text-xl font-bold text-sky-900">{word.word}</p>
                <p className="text-sky-700">{word.definition}</p>
                {word.example && <p className="text-sm italic text-sky-500">"{word.example}"</p>}

                <label className="mt-3 flex items-center gap-2 text-sm font-semibold text-sky-900">
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
                    className="rounded-xl bg-emerald-500 px-4 py-2 text-sm font-bold text-white shadow hover:bg-emerald-600 disabled:opacity-60"
                  >
                    Approve
                  </button>
                  <button
                    type="button"
                    onClick={() => moderate.mutate({ id: word.id, payload: { approve: false, visibleToChildren: false } })}
                    disabled={moderate.isPending}
                    className="rounded-xl bg-rose-100 px-4 py-2 text-sm font-bold text-rose-700 hover:bg-rose-200 disabled:opacity-60"
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
