import type { ReactElement } from 'react'
import { useState } from 'react'
import type { ExerciseOption, VocabularyExercise } from '../../types'
import { ChoiceOptions } from './ChoiceOptions'
import type { ExerciseAnswer } from './sessionQueue'
import { useResponseTimer } from './useResponseTimer'

interface PictureChoiceExerciseProps {
  exercise: VocabularyExercise
  onAnswer: (answer: ExerciseAnswer) => void
}

/** Shows the exercise picture; the learner picks the matching word. Hint = show the definition. */
export function PictureChoiceExercise({ exercise, onAnswer }: PictureChoiceExerciseProps): ReactElement {
  const elapsed = useResponseTimer()
  const [hintUsed, setHintUsed] = useState(false)
  const [answered, setAnswered] = useState(false)
  const { prompt } = exercise

  function handlePick(option: ExerciseOption): void {
    setAnswered(true)
    onAnswer({ answer: { optionKey: option.key }, clientResponseMs: elapsed(), hintUsed })
  }

  return (
    <div data-testid="exercise-picture-choice">
      <p className="text-wb-ink-muted">Which word matches the picture?</p>
      {prompt.imageUrl && (
        <img src={prompt.imageUrl} alt="What is this?" className="mx-auto mt-4 max-h-56 rounded-wb-card object-contain" />
      )}
      {hintUsed ? (
        <p className="mt-3 text-wb-ink-muted">{prompt.definition}</p>
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
      <ChoiceOptions options={exercise.options} disabled={answered} onPick={handlePick} />
    </div>
  )
}
