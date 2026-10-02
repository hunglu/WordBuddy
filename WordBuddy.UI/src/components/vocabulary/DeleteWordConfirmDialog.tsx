import { AnimatePresence, motion } from 'framer-motion'
import type { ReactElement } from 'react'
import type { PersonalVocabularyWord } from '../../types'

type DeleteWordConfirmDialogProps = Readonly<{
  /** The word to delete, or `null` when the dialog is closed. */
  word: PersonalVocabularyWord | null
  isPending: boolean
  onConfirm: () => void
  onCancel: () => void
}>

/** Text shown for an author delete that needs confirmation (`Shared` or `PendingReview` words). */
function confirmMessage(word: PersonalVocabularyWord): string {
  return word.shareStatus === 'Shared'
    ? 'This word will be handed over to WordBuddy and stay in the Community Word Pool. It will no longer be yours.'
    : 'Your share request will be cancelled and the word deleted.'
}

/** Whether deleting this word needs an explicit confirmation (author of a shared or pending word). */
export function needsDeleteConfirmation(word: PersonalVocabularyWord): boolean {
  return word.isAuthor && (word.shareStatus === 'Shared' || word.shareStatus === 'PendingReview')
}

/** Modal that asks the author to confirm deleting a shared or pending word. */
export function DeleteWordConfirmDialog({ word, isPending, onConfirm, onCancel }: DeleteWordConfirmDialogProps): ReactElement {
  return (
    <AnimatePresence>
      {word && (
        <motion.div
          key="delete-word-backdrop"
          className="fixed inset-0 z-50 flex items-center justify-center bg-wb-ink/40 p-4"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          transition={{ duration: 0.2 }}
        >
          <motion.div
            role="dialog"
            aria-modal="true"
            aria-labelledby="delete-word-title"
            aria-describedby="delete-word-message"
            className="w-full max-w-md rounded-wb-card bg-wb-surface-card p-6 shadow-wb-overlay"
            initial={{ opacity: 0, y: 16 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: 16 }}
            transition={{ duration: 0.2 }}
          >
            <h2 id="delete-word-title" className="text-xl font-bold text-wb-ink">
              Delete "{word.word}"?
            </h2>
            <p id="delete-word-message" className="mt-3 text-wb-ink-muted">
              {confirmMessage(word)}
            </p>
            <div className="mt-6 flex justify-end gap-2">
              <button
                type="button"
                autoFocus
                onClick={onCancel}
                disabled={isPending}
                className="rounded-wb-md bg-wb-primary-soft px-4 py-2 text-sm font-semibold text-wb-ink hover:bg-wb-primary-soft-hover disabled:opacity-60"
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={onConfirm}
                disabled={isPending}
                className="rounded-wb-md bg-wb-danger-soft px-4 py-2 text-sm font-semibold text-wb-danger-soft-ink hover:bg-wb-danger-soft-hover disabled:opacity-60"
              >
                {isPending ? 'Deleting…' : 'Delete'}
              </button>
            </div>
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  )
}
