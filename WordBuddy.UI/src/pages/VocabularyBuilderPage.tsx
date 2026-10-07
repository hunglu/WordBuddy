import { zodResolver } from '@hookform/resolvers/zod'
import axios from 'axios'
import { motion } from 'framer-motion'
import { useState, type ReactElement } from 'react'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router-dom'
import { z } from 'zod'
import { DeleteWordConfirmDialog, needsDeleteConfirmation } from '../components/vocabulary/DeleteWordConfirmDialog'
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
  const [wordToConfirm, setWordToConfirm] = useState<PersonalVocabularyWord | null>(null)

  const onDelete = (word: PersonalVocabularyWord): void => {
    deleteWord.reset()
    if (needsDeleteConfirmation(word)) {
      setWordToConfirm(word)
      return
    }
    deleteWord.mutate(
      { id: word.id },
      {
        onError: (error) => {
          // The word was shared elsewhere since the list loaded: the API asks for confirmation.
          if (axios.isAxiosError(error) && error.response?.status === 409) {
            deleteWord.reset()
            setWordToConfirm(word)
          }
        },
      },
    )
  }

  const onConfirmDelete = (): void => {
    if (!wordToConfirm) return
    deleteWord.mutate({ id: wordToConfirm.id, confirm: true }, { onSuccess: () => setWordToConfirm(null) })
  }

  const onCancelDelete = (): void => {
    deleteWord.reset()
    setWordToConfirm(null)
  }

  const deleteErrorMessage = "Couldn't delete this word right now. Please try again."

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
            to="/vocabulary/review"
            className="rounded-wb-md bg-wb-success px-4 py-2 text-sm font-bold text-wb-on-success hover:bg-wb-success-hover shadow-wb-card"
          >
            Start Review
          </Link>
          <Link
            to="/vocabulary/shared"
            className="rounded-wb-md bg-wb-primary-soft px-4 py-2 text-sm font-bold text-wb-ink hover:bg-wb-primary-soft-hover"
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
          className="self-start rounded-wb-md bg-wb-primary px-6 py-3 text-lg font-bold text-wb-on-primary shadow-wb-card hover:bg-wb-primary-hover disabled:opacity-60"
        >
          {addWord.isPending ? 'Adding…' : 'Add word'}
        </button>
      </form>

      {isLoading && <p className="mt-8 text-lg text-wb-ink-muted">Loading your words…</p>}
      {isError && <p className="mt-8 text-lg text-wb-danger">Couldn't load your vocabulary right now.</p>}

      {deleteWord.isError && !wordToConfirm && (
        <p role="alert" className="mt-4 text-lg text-wb-danger">
          {deleteErrorMessage}
        </p>
      )}

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
                {user?.ageGroup !== 'Child' && word.isAuthor && (word.shareStatus === 'Private' || word.shareStatus === 'Rejected') && (
                  <button
                    type="button"
                    onClick={() => requestShare.mutate(word.id)}
                    disabled={requestShare.isPending}
                    className="rounded-wb-md bg-wb-success-soft px-3 py-1.5 text-sm font-semibold text-wb-success-soft-ink hover:bg-wb-success-soft-hover disabled:opacity-60"
                  >
                    Share
                  </button>
                )}
                <button
                  type="button"
                  onClick={() => onDelete(word)}
                  disabled={deleteWord.isPending}
                  className="rounded-wb-md bg-wb-danger-soft px-3 py-1.5 text-sm font-semibold text-wb-danger-soft-ink hover:bg-wb-danger-soft-hover disabled:opacity-60"
                >
                  Delete
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      <DeleteWordConfirmDialog
        word={wordToConfirm}
        isPending={deleteWord.isPending}
        errorMessage={deleteWord.isError ? deleteErrorMessage : null}
        onConfirm={onConfirmDelete}
        onCancel={onCancelDelete}
      />
    </motion.div>
  )
}
