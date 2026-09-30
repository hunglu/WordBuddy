import { zodResolver } from '@hookform/resolvers/zod'
import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router-dom'
import { z } from 'zod'
import {
  useAddVocabularyWord,
  useDeleteVocabularyWord,
  useMyVocabularyWords,
  useRequestShareVocabularyWord,
} from '../hooks/useVocabulary'
import { useAuthStore } from '../store/authStore'
import { shareStatusClasses } from '../theme/variants'
import type { PersonalVocabularyWord } from '../types'

const wordSchema = z.object({
  word: z.string().min(1, 'Word is required').max(200),
  definition: z.string().min(1, 'Definition is required').max(2000),
  example: z.string().max(500).optional(),
})

type WordFormValues = z.infer<typeof wordSchema>

export function VocabularyBuilderPage(): ReactElement {
  const user = useAuthStore((state) => state.user)
  const { data: words, isLoading, isError } = useMyVocabularyWords()
  const addWord = useAddVocabularyWord()
  const deleteWord = useDeleteVocabularyWord()
  const requestShare = useRequestShareVocabularyWord()

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<WordFormValues>({ resolver: zodResolver(wordSchema) })

  const onSubmit = handleSubmit((values) => {
    addWord.mutate(
      { word: values.word, definition: values.definition, example: values.example || undefined },
      { onSuccess: () => reset() },
    )
  })

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-3xl font-extrabold text-wb-ink">My Vocabulary</h1>
        <div className="flex gap-2">
          <Link
            to="/vocabulary/check"
            className="rounded-wb-md bg-wb-success px-4 py-2 text-sm font-bold text-wb-on-success shadow"
          >
            Start Recall Check
          </Link>
          <Link
            to="/vocabulary/shared"
            className="rounded-wb-md bg-sky-100 px-4 py-2 text-sm font-bold text-wb-ink hover:bg-sky-200"
          >
            Browse Shared Pool
          </Link>
        </div>
      </div>

      <form onSubmit={onSubmit} className="mt-6 flex flex-col gap-4 rounded-wb-card bg-wb-surface-card p-6 shadow-wb-card">
        <h2 className="text-xl font-bold text-wb-ink">Add a word</h2>

        <div>
          <label htmlFor="word" className="mb-1 block text-sm font-semibold text-wb-ink">
            Word
          </label>
          <input
            id="word"
            className="w-full rounded-wb-md border-2 border-wb-border-control px-4 py-3 text-base focus:border-wb-primary"
            {...register('word')}
          />
          {errors.word && <p className="mt-1 text-sm text-wb-danger">{errors.word.message}</p>}
        </div>

        <div>
          <label htmlFor="definition" className="mb-1 block text-sm font-semibold text-wb-ink">
            Definition
          </label>
          <textarea
            id="definition"
            rows={2}
            className="w-full rounded-wb-md border-2 border-wb-border-control px-4 py-3 text-base focus:border-wb-primary"
            {...register('definition')}
          />
          {errors.definition && <p className="mt-1 text-sm text-wb-danger">{errors.definition.message}</p>}
        </div>

        <div>
          <label htmlFor="example" className="mb-1 block text-sm font-semibold text-wb-ink">
            Example (optional)
          </label>
          <textarea
            id="example"
            rows={2}
            className="w-full rounded-wb-md border-2 border-wb-border-control px-4 py-3 text-base focus:border-wb-primary"
            {...register('example')}
          />
          {errors.example && <p className="mt-1 text-sm text-wb-danger">{errors.example.message}</p>}
        </div>

        <button
          type="submit"
          disabled={addWord.isPending}
          className="self-start rounded-wb-md bg-wb-primary px-6 py-3 text-lg font-bold text-wb-on-primary shadow hover:bg-wb-primary-hover disabled:opacity-60"
        >
          {addWord.isPending ? 'Adding…' : 'Add word'}
        </button>
      </form>

      {isLoading && <p className="mt-8 text-lg text-wb-ink-muted">Loading your words…</p>}
      {isError && <p className="mt-8 text-lg text-wb-danger">Couldn't load your vocabulary right now.</p>}

      {words && words.length === 0 && (
        <p className="mt-8 text-lg text-wb-ink-muted">You haven't added any words yet — start above!</p>
      )}

      {words && words.length > 0 && (
        <div className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {words.map((word: PersonalVocabularyWord) => (
            <div key={word.id} className="flex flex-col gap-2 rounded-wb-card bg-wb-surface-card p-5 shadow-wb-card">
              <div className="flex items-start justify-between gap-2">
                <p className="text-xl font-bold text-wb-ink">{word.word}</p>
                <span
                  className={`whitespace-nowrap rounded-wb-pill border px-3 py-1 text-xs font-semibold ${shareStatusClasses[word.shareStatus]}`}
                >
                  {word.shareStatus}
                </span>
              </div>
              <p className="text-wb-ink-muted">{word.definition}</p>
              {word.example && <p className="text-sm italic text-wb-ink-muted">"{word.example}"</p>}

              <div className="mt-2 flex gap-2">
                {user?.ageGroup !== 'Child' && (word.shareStatus === 'Private' || word.shareStatus === 'Rejected') && (
                  <button
                    type="button"
                    onClick={() => requestShare.mutate(word.id)}
                    disabled={requestShare.isPending}
                    className="rounded-wb-md bg-emerald-100 px-3 py-1.5 text-sm font-semibold text-emerald-800 hover:bg-emerald-200 disabled:opacity-60"
                  >
                    Share
                  </button>
                )}
                <button
                  type="button"
                  onClick={() => deleteWord.mutate(word.id)}
                  disabled={deleteWord.isPending}
                  className="rounded-wb-md bg-wb-danger-soft px-3 py-1.5 text-sm font-semibold text-wb-danger hover:bg-rose-200 disabled:opacity-60"
                >
                  Delete
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </motion.div>
  )
}
