import { useQuery } from '@tanstack/react-query'
import { AnimatePresence, motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { getRandomWordsForCheck } from '../api/vocabulary'
import { useSubmitVocabularyRecallCheck } from '../hooks/useVocabulary'
import { useVocabularyCheckStore } from '../store/vocabularyCheckStore'
import type { VocabularyRecallResultItem } from '../types'

const COUNT_OPTIONS = [5, 10, 15, 20]

export function VocabularyCheckPage(): ReactElement {
  const { lastUsedCount, setLastUsedCount } = useVocabularyCheckStore()
  const [started, setStarted] = useState(false)
  const [currentIndex, setCurrentIndex] = useState(0)
  const [results, setResults] = useState<VocabularyRecallResultItem[]>([])

  const {
    data: words,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ['vocabulary', 'check', lastUsedCount],
    queryFn: () => getRandomWordsForCheck(lastUsedCount),
    enabled: started,
  })

  const submitCheck = useSubmitVocabularyRecallCheck()

  function handleStart(): void {
    setResults([])
    setCurrentIndex(0)
    setStarted(true)
  }

  function handleAnswer(known: boolean): void {
    if (!words) {
      return
    }

    const current = words[currentIndex]
    const nextResults = [...results, { vocabularyWordId: current.id, word: current.word, known }]
    setResults(nextResults)

    if (currentIndex + 1 < words.length) {
      setCurrentIndex(currentIndex + 1)
    } else {
      submitCheck.mutate(nextResults)
    }
  }

  function handleRestart(): void {
    setStarted(false)
    setResults([])
    setCurrentIndex(0)
  }

  if (!started) {
    return (
      <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
        <h1 className="text-3xl font-extrabold text-wb-ink">Recall Check</h1>
        <p className="mt-2 text-wb-ink-muted">Pick how many words to check today.</p>

        <div className="mt-6 flex flex-wrap gap-2">
          {COUNT_OPTIONS.map((option) => (
            <button
              key={option}
              type="button"
              onClick={() => setLastUsedCount(option)}
              className={`rounded-wb-pill px-5 py-2 text-base font-bold ${
                lastUsedCount === option ? 'bg-wb-primary text-wb-on-primary shadow-wb-card' : 'bg-wb-surface-card text-wb-ink hover:bg-wb-hover-tint'
              }`}
            >
              {option} words
            </button>
          ))}
        </div>

        <button
          type="button"
          onClick={handleStart}
          className="mt-8 rounded-wb-md bg-wb-success px-6 py-3 text-lg font-bold text-wb-on-success hover:bg-wb-success-hover shadow-wb-card"
        >
          Start Check
        </button>
      </motion.div>
    )
  }

  if (isLoading) {
    return <p className="text-lg text-wb-ink-muted">Picking words for your check…</p>
  }

  if (isError || !words) {
    return <p className="text-lg text-wb-danger">Couldn't start a check right now. Please try again.</p>
  }

  if (words.length === 0) {
    return (
      <div>
        <p className="text-lg text-wb-ink-muted">Add some words to your list first!</p>
        <Link to="/vocabulary" className="mt-4 inline-block font-semibold text-wb-ink-muted hover:underline">
          Back to My Vocabulary
        </Link>
      </div>
    )
  }

  const isFinished = currentIndex >= words.length || submitCheck.isSuccess

  if (isFinished) {
    const wordsKnown = results.filter((r) => r.known).length

    return (
      <motion.div
        initial={{ opacity: 0, scale: 0.95 }}
        animate={{ opacity: 1, scale: 1 }}
        transition={{ duration: 0.3 }}
        className="flex flex-col items-center gap-3 rounded-wb-card bg-wb-surface-card p-10 text-center shadow-wb-card"
      >
        <span className="text-5xl">🎉</span>
        <p className="text-2xl font-bold text-wb-ink">Check complete!</p>
        <p className="text-wb-ink-muted">
          You knew {wordsKnown} of {results.length} words.
        </p>
        <div className="mt-4 flex gap-3">
          <button
            type="button"
            onClick={handleRestart}
            className="rounded-wb-md bg-wb-primary px-6 py-3 text-lg font-bold text-wb-on-primary shadow-wb-card hover:bg-wb-primary-hover"
          >
            Check Again
          </button>
          <Link
            to="/progress"
            className="rounded-wb-md bg-wb-primary-soft px-6 py-3 text-lg font-bold text-wb-ink hover:bg-wb-primary-soft-hover"
          >
            View Progress
          </Link>
        </div>
      </motion.div>
    )
  }

  const current = words[currentIndex]

  return (
    <div className="flex flex-col items-center">
      <p className="mb-4 text-wb-ink-muted">
        Word {currentIndex + 1} of {words.length}
      </p>

      <AnimatePresence mode="wait">
        <motion.div
          key={current.id}
          initial={{ opacity: 0, x: 40 }}
          animate={{ opacity: 1, x: 0 }}
          exit={{ opacity: 0, x: -40 }}
          transition={{ duration: 0.25 }}
          className="w-full max-w-md rounded-wb-card bg-wb-surface-card p-8 text-center shadow-wb-card"
        >
          <p className="text-3xl font-extrabold text-wb-ink">{current.word}</p>
          <p className="mt-3 text-wb-ink-muted">{current.definition}</p>
          {current.example && <p className="mt-2 text-sm italic text-wb-ink-muted">"{current.example}"</p>}
        </motion.div>
      </AnimatePresence>

      <div className="mt-6 flex gap-4">
        <button
          type="button"
          onClick={() => handleAnswer(false)}
          className="rounded-wb-md bg-wb-secondary px-6 py-3 text-lg font-bold text-wb-on-secondary shadow-wb-card hover:bg-wb-secondary-hover"
        >
          Still Learning
        </button>
        <button
          type="button"
          onClick={() => handleAnswer(true)}
          className="rounded-wb-md bg-wb-success px-6 py-3 text-lg font-bold text-wb-on-success hover:bg-wb-success-hover shadow-wb-card"
        >
          I Know This
        </button>
      </div>
    </div>
  )
}
