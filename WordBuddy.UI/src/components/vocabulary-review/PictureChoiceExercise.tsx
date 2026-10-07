import type { ReactElement } from 'react'
import { useState } from 'react'
import type { SenseReview } from '../../types'
import { ChoiceOptions } from './ChoiceOptions'
import type { ExerciseAnswer } from './sessionQueue'
import { useResponseTimer } from './useResponseTimer'

interface PictureChoiceExerciseProps {
  sense: SenseReview
  options: SenseReview[]
  onAnswer: (answer: ExerciseAnswer) => void
}

/** Shows the sense picture; the learner picks the matching word. Hint = show the definition. */
export function PictureChoiceExercise({ sense, options, onAnswer }: PictureChoiceExerciseProps): ReactElement {
  const elapsed = useResponseTimer()
  const [hintUsed, setHintUsed] = useState(false)
  const [answered, setAnswered] = useState(false)

  function handlePick(option: SenseReview): void {
    setAnswered(true)
    onAnswer({ isCorrect: option.senseId === sense.senseId, responseMs: elapsed(), hintUsed })
  }

  return (
    <div data-testid="exercise-picture-choice">
      <p className="text-wb-ink-muted">Which word matches the picture?</p>
      {sense.imageUrl && (
        <img src={sense.imageUrl} alt="What is this?" className="mx-auto mt-4 max-h-56 rounded-wb-card object-contain" />
      )}
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
      <ChoiceOptions options={options} disabled={answered} onPick={handlePick} />
    </div>
  )
}
