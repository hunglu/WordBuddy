import type { FormEvent, ReactElement } from 'react'
import { useState } from 'react'
import type { VocabularyExercise } from '../../types'
import type { ExerciseAnswer } from './sessionQueue'
import { useResponseTimer } from './useResponseTimer'

interface TypingExerciseProps {
  exercise: VocabularyExercise
  onAnswer: (answer: ExerciseAnswer) => void
}

/** Shows the definition; the learner types the word. The server checks it. Hint = first letter. */
export function TypingExercise({ exercise, onAnswer }: TypingExerciseProps): ReactElement {
  const elapsed = useResponseTimer()
  const [typed, setTyped] = useState('')
  const [hintUsed, setHintUsed] = useState(false)
  const [answered, setAnswered] = useState(false)
  const { prompt } = exercise

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault()
    if (answered || typed.trim() === '') {
      return
    }
    setAnswered(true)
    onAnswer({ answer: { text: typed }, clientResponseMs: elapsed(), hintUsed })
  }

  return (
    <form data-testid="exercise-typing" onSubmit={handleSubmit}>
      <p className="text-wb-ink-muted">Type the word for this meaning.</p>
      <p className="mt-4 text-xl font-semibold text-wb-ink">{prompt.definition}</p>
      {hintUsed ? (
        <p className="mt-3 text-wb-ink-muted">
          Starts with <span className="font-bold text-wb-ink">{prompt.hintFirstLetter ?? ''}</span>
        </p>
      ) : (
        <button
          type="button"
          onClick={() => setHintUsed(true)}
          disabled={answered}
          className="mt-3 text-sm font-semibold text-wb-ink-muted hover:underline"
        >
          Show a hint
        </button>
      )}
      <input
        aria-label="Your answer"
        value={typed}
        onChange={(event) => setTyped(event.target.value)}
        disabled={answered}
        autoComplete="off"
        autoFocus
        className="mt-4 w-full rounded-wb-md border border-wb-border-control px-4 py-3 text-lg text-wb-ink focus:outline-none focus:ring-2 focus:ring-wb-focus-ring"
      />
      <button
        type="submit"
        disabled={answered}
        className="mt-4 rounded-wb-md bg-wb-success px-6 py-3 text-lg font-bold text-wb-on-success shadow-wb-card hover:bg-wb-success-hover disabled:opacity-60"
      >
        Check
      </button>
    </form>
  )
}
