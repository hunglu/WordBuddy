import type { ReactElement } from 'react'

interface PersonalContextNoteProps {
  personalContext: string | null
}

/** The learner's own note on where they met the word. Renders nothing when not set. */
export function PersonalContextNote({ personalContext }: PersonalContextNoteProps): ReactElement | null {
  if (!personalContext) {
    return null
  }

  return (
    <p data-testid="personal-context" className="mt-4 rounded-wb-md bg-wb-primary-soft px-4 py-2 text-sm text-wb-ink">
      📌 {personalContext}
    </p>
  )
}
