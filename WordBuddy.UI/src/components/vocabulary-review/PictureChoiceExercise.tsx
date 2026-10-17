import type { ReactElement } from 'react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
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
  const { t } = useTranslation('review')
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
      <p className="text-wb-ink-muted">{t('picture.question')}</p>
      {prompt.imageUrl && (
        <img src={prompt.imageUrl} alt={t('picture.alt')} className="mx-auto mt-4 max-h-56 rounded-wb-card object-contain" />
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
          {t('hint')}
        </button>
      )}
      <ChoiceOptions options={exercise.options} disabled={answered} onPick={handlePick} />
    </div>
  )
}
