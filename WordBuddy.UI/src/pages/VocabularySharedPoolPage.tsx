import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useAddSharedWordToMyList, useSharedVocabularyWords } from '../hooks/useVocabulary'

export function VocabularySharedPoolPage(): ReactElement {
  const { data: words, isLoading, isError } = useSharedVocabularyWords()
  const addToMyList = useAddSharedWordToMyList()

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <h1 className="text-3xl font-extrabold text-wb-ink">Community Word Pool</h1>
      <p className="mt-2 text-wb-ink-muted">Words other learners have shared and a moderator has approved.</p>

      {isLoading && <p className="mt-8 text-lg text-wb-ink-muted">Loading the shared pool…</p>}
      {isError && <p className="mt-8 text-lg text-wb-danger">Couldn't load the shared pool right now.</p>}

      {words && words.length === 0 && (
        <p className="mt-8 text-lg text-wb-ink-muted">No shared words yet — check back soon!</p>
      )}

      {words && words.length > 0 && (
        <div className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {words.map((word) => (
            <div key={word.id} className="flex flex-col gap-2 rounded-wb-card bg-wb-surface-card p-5 shadow-wb-card">
              <p className="text-xl font-bold text-wb-ink">{word.word}</p>
              <p className="text-wb-ink-muted">{word.definition}</p>
              {word.example && <p className="text-sm italic text-wb-ink-muted">"{word.example}"</p>}

              <button
                type="button"
                onClick={() => addToMyList.mutate(word.id)}
                disabled={addToMyList.isPending}
                className="mt-2 self-start rounded-wb-md bg-wb-primary px-4 py-2 text-sm font-bold text-wb-on-primary shadow hover:bg-wb-primary-hover disabled:opacity-60"
              >
                Add to My List
              </button>
            </div>
          ))}
        </div>
      )}
    </motion.div>
  )
}
