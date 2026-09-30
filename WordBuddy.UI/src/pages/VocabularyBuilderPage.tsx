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
import type { PersonalVocabularyWord, VocabularyShareStatus } from '../types'

const wordSchema = z.object({
  word: z.string().min(1, 'Word is required').max(200),
  definition: z.string().min(1, 'Definition is required').max(2000),
  example: z.string().max(500).optional(),
})

type WordFormValues = z.infer<typeof wordSchema>

const STATUS_COLORS: Record<VocabularyShareStatus, string> = {
  Private: 'bg-sky-100 text-sky-800 border-sky-300',
  PendingReview: 'bg-amber-100 text-amber-800 border-amber-300',
  Shared: 'bg-emerald-100 text-emerald-800 border-emerald-300',
  Rejected: 'bg-rose-100 text-rose-800 border-rose-300',
}

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
        <h1 className="text-3xl font-extrabold text-sky-900">My Vocabulary</h1>
        <div className="flex gap-2">
          <Link
            to="/vocabulary/check"
            className="rounded-xl bg-emerald-500 px-4 py-2 text-sm font-bold text-white shadow hover:bg-emerald-600"
          >
            Start Recall Check
          </Link>
          <Link
            to="/vocabulary/shared"
            className="rounded-xl bg-sky-100 px-4 py-2 text-sm font-bold text-sky-800 hover:bg-sky-200"
          >
            Browse Shared Pool
          </Link>
        </div>
      </div>

      <form onSubmit={onSubmit} className="mt-6 flex flex-col gap-4 rounded-3xl bg-white p-6 shadow-sm">
        <h2 className="text-xl font-bold text-sky-900">Add a word</h2>

        <div>
          <label htmlFor="word" className="mb-1 block text-sm font-semibold text-sky-900">
            Word
          </label>
          <input
            id="word"
            className="w-full rounded-xl border-2 border-sky-200 px-4 py-3 text-base focus:border-sky-500"
            {...register('word')}
          />
          {errors.word && <p className="mt-1 text-sm text-rose-600">{errors.word.message}</p>}
        </div>

        <div>
          <label htmlFor="definition" className="mb-1 block text-sm font-semibold text-sky-900">
            Definition
          </label>
          <textarea
            id="definition"
            rows={2}
            className="w-full rounded-xl border-2 border-sky-200 px-4 py-3 text-base focus:border-sky-500"
            {...register('definition')}
          />
          {errors.definition && <p className="mt-1 text-sm text-rose-600">{errors.definition.message}</p>}
        </div>

        <div>
          <label htmlFor="example" className="mb-1 block text-sm font-semibold text-sky-900">
            Example (optional)
          </label>
          <textarea
            id="example"
            rows={2}
            className="w-full rounded-xl border-2 border-sky-200 px-4 py-3 text-base focus:border-sky-500"
            {...register('example')}
          />
          {errors.example && <p className="mt-1 text-sm text-rose-600">{errors.example.message}</p>}
        </div>

        <button
          type="submit"
          disabled={addWord.isPending}
          className="self-start rounded-xl bg-sky-500 px-6 py-3 text-lg font-bold text-white shadow transition-colors hover:bg-sky-600 disabled:opacity-60"
        >
          {addWord.isPending ? 'Adding…' : 'Add word'}
        </button>
      </form>

      {isLoading && <p className="mt-8 text-lg text-sky-600">Loading your words…</p>}
      {isError && <p className="mt-8 text-lg text-rose-600">Couldn't load your vocabulary right now.</p>}

      {words && words.length === 0 && (
        <p className="mt-8 text-lg text-sky-600">You haven't added any words yet — start above!</p>
      )}

      {words && words.length > 0 && (
        <div className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {words.map((word: PersonalVocabularyWord) => (
            <div key={word.id} className="flex flex-col gap-2 rounded-3xl bg-white p-5 shadow-sm">
              <div className="flex items-start justify-between gap-2">
                <p className="text-xl font-bold text-sky-900">{word.word}</p>
                <span
                  className={`whitespace-nowrap rounded-full border px-3 py-1 text-xs font-semibold ${STATUS_COLORS[word.shareStatus]}`}
                >
                  {word.shareStatus}
                </span>
              </div>
              <p className="text-sky-700">{word.definition}</p>
              {word.example && <p className="text-sm italic text-sky-500">"{word.example}"</p>}

              <div className="mt-2 flex gap-2">
                {user?.ageGroup !== 'Child' && (word.shareStatus === 'Private' || word.shareStatus === 'Rejected') && (
                  <button
                    type="button"
                    onClick={() => requestShare.mutate(word.id)}
                    disabled={requestShare.isPending}
                    className="rounded-xl bg-emerald-100 px-3 py-1.5 text-sm font-semibold text-emerald-800 hover:bg-emerald-200 disabled:opacity-60"
                  >
                    Share
                  </button>
                )}
                <button
                  type="button"
                  onClick={() => deleteWord.mutate(word.id)}
                  disabled={deleteWord.isPending}
                  className="rounded-xl bg-rose-100 px-3 py-1.5 text-sm font-semibold text-rose-700 hover:bg-rose-200 disabled:opacity-60"
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
