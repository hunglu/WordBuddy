import type { ReactElement } from 'react'
import { useState } from 'react'
import type { SenseReview } from '../../types'
import { ChoiceOptions } from './ChoiceOptions'
import type { ExerciseAnswer } from './sessionQueue'
import { useResponseTimer } from './useResponseTimer'

interface ListeningChoiceExerciseProps {
  sense: SenseReview
  options: SenseReview[]
  onAnswer: (answer: ExerciseAnswer) => void
}

function play(url: string | null): void {
  if (url) {
    void new Audio(url).play().catch(() => undefined)
  }
}

/** Plays the word audio; the learner picks the word they heard. Hint = show the definition. */
export function ListeningChoiceExercise({ sense, options, onAnswer }: ListeningChoiceExerciseProps): ReactElement {
  const elapsed = useResponseTimer()
  const [hintUsed, setHintUsed] = useState(false)
  const [answered, setAnswered] = useState(false)

  function handlePick(option: SenseReview): void {
    setAnswered(true)
    onAnswer({ isCorrect: option.senseId === sense.senseId, responseMs: elapsed(), hintUsed })
  }

  return (
    <div data-testid="exercise-listening-choice">
      <p className="text-wb-ink-muted">Listen and pick the word you hear.</p>
      <button
        type="button"
        onClick={() => play(sense.audioUrl)}
        className="mt-4 rounded-wb-pill bg-wb-primary px-6 py-3 text-lg font-bold text-wb-on-primary shadow-wb-card hover:bg-wb-primary-hover"
      >
        🔊 Play
      </button>
      <div>
        {hintUsed ? (
          <p className="mt-3 text-wb-ink-muted">{sense.definition}</p>
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
      </div>
      <ChoiceOptions options={options} disabled={answered} onPick={handlePick} />
    </div>
  )
}
