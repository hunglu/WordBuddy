import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useAddSharedWordToMyList, useSharedVocabularyWords } from '../hooks/useVocabulary'

export function VocabularySharedPoolPage(): ReactElement {
  const { data: words, isLoading, isError } = useSharedVocabularyWords()
  const addToMyList = useAddSharedWordToMyList()

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <h1 className="text-3xl font-extrabold text-sky-900">Community Word Pool</h1>
      <p className="mt-2 text-sky-700">Words other learners have shared and a moderator has approved.</p>

      {isLoading && <p className="mt-8 text-lg text-sky-600">Loading the shared pool…</p>}
      {isError && <p className="mt-8 text-lg text-rose-600">Couldn't load the shared pool right now.</p>}

      {words && words.length === 0 && (
        <p className="mt-8 text-lg text-sky-600">No shared words yet — check back soon!</p>
      )}

      {words && words.length > 0 && (
        <div className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {words.map((word) => (
            <div key={word.id} className="flex flex-col gap-2 rounded-3xl bg-white p-5 shadow-sm">
              <p className="text-xl font-bold text-sky-900">{word.word}</p>
              <p className="text-sky-700">{word.definition}</p>
              {word.example && <p className="text-sm italic text-sky-500">"{word.example}"</p>}

              <button
                type="button"
                onClick={() => addToMyList.mutate(word.id)}
                disabled={addToMyList.isPending}
                className="mt-2 self-start rounded-xl bg-sky-500 px-4 py-2 text-sm font-bold text-white shadow hover:bg-sky-600 disabled:opacity-60"
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
