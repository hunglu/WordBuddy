import type { ReactElement } from 'react'
import type { SenseReview } from '../../types'

interface ChoiceOptionsProps {
  options: SenseReview[]
  disabled: boolean
  onPick: (option: SenseReview) => void
}

/** The 4 word buttons shared by the choice exercises. */
export function ChoiceOptions({ options, disabled, onPick }: ChoiceOptionsProps): ReactElement {
  return (
    <div className="mt-6 grid grid-cols-2 gap-3">
      {options.map((option) => (
        <button
          key={option.senseId}
          type="button"
          disabled={disabled}
          onClick={() => onPick(option)}
          className="rounded-wb-md bg-wb-primary-soft px-4 py-3 text-lg font-bold text-wb-ink hover:bg-wb-primary-soft-hover disabled:opacity-60"
        >
          {option.word}
        </button>
      ))}
    </div>
  )
}
