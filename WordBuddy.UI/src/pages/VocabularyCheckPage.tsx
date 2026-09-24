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
        <h1 className="text-3xl font-extrabold text-sky-900">Recall Check</h1>
        <p className="mt-2 text-sky-700">Pick how many words to check today.</p>

        <div className="mt-6 flex flex-wrap gap-2">
          {COUNT_OPTIONS.map((option) => (
            <button
              key={option}
              type="button"
              onClick={() => setLastUsedCount(option)}
              className={`rounded-full px-5 py-2 text-base font-bold transition-colors ${
                lastUsedCount === option ? 'bg-sky-500 text-white shadow' : 'bg-white text-sky-900 hover:bg-sky-100'
              }`}
            >
              {option} words
            </button>
          ))}
        </div>

        <button
          type="button"
          onClick={handleStart}
          className="mt-8 rounded-xl bg-emerald-500 px-6 py-3 text-lg font-bold text-white shadow hover:bg-emerald-600"
        >
          Start Check
        </button>
      </motion.div>
    )
  }

  if (isLoading) {
    return <p className="text-lg text-sky-600">Picking words for your check…</p>
  }

  if (isError || !words) {
    return <p className="text-lg text-rose-600">Couldn't start a check right now. Please try again.</p>
  }

  if (words.length === 0) {
    return (
      <div>
        <p className="text-lg text-sky-600">Add some words to your list first!</p>
        <Link to="/vocabulary" className="mt-4 inline-block font-semibold text-sky-600 hover:underline">
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
        className="flex flex-col items-center gap-3 rounded-3xl bg-white p-10 text-center shadow-sm"
      >
        <span className="text-5xl">🎉</span>
        <p className="text-2xl font-bold text-sky-900">Check complete!</p>
        <p className="text-sky-700">
          You knew {wordsKnown} of {results.length} words.
        </p>
        <div className="mt-4 flex gap-3">
          <button
            type="button"
            onClick={handleRestart}
            className="rounded-xl bg-sky-500 px-6 py-3 text-lg font-bold text-white shadow hover:bg-sky-600"
          >
            Check Again
          </button>
          <Link
            to="/progress"
            className="rounded-xl bg-sky-100 px-6 py-3 text-lg font-bold text-sky-800 hover:bg-sky-200"
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
      <p className="mb-4 text-sky-600">
        Word {currentIndex + 1} of {words.length}
      </p>

      <AnimatePresence mode="wait">
        <motion.div
          key={current.id}
          initial={{ opacity: 0, x: 40 }}
          animate={{ opacity: 1, x: 0 }}
          exit={{ opacity: 0, x: -40 }}
          transition={{ duration: 0.25 }}
          className="w-full max-w-md rounded-3xl bg-white p-8 text-center shadow-sm"
        >
          <p className="text-3xl font-extrabold text-sky-900">{current.word}</p>
          <p className="mt-3 text-sky-700">{current.definition}</p>
          {current.example && <p className="mt-2 text-sm italic text-sky-500">"{current.example}"</p>}
        </motion.div>
      </AnimatePresence>

      <div className="mt-6 flex gap-4">
        <button
          type="button"
          onClick={() => handleAnswer(false)}
          className="rounded-xl bg-amber-400 px-6 py-3 text-lg font-bold text-white shadow hover:bg-amber-500"
        >
          Still Learning
        </button>
        <button
          type="button"
          onClick={() => handleAnswer(true)}
          className="rounded-xl bg-emerald-500 px-6 py-3 text-lg font-bold text-white shadow hover:bg-emerald-600"
        >
          I Know This
        </button>
      </div>
    </div>
  )
}
