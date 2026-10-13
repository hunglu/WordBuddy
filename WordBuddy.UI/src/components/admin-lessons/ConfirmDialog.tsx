import { AnimatePresence, motion } from 'framer-motion'
import { useEffect, useRef, type KeyboardEvent, type ReactElement } from 'react'

type ConfirmDialogProps = Readonly<{
  /** Whether the dialog is shown. */
  open: boolean
  title: string
  message: string
  confirmLabel: string
  pendingLabel: string
  isPending: boolean
  /** Error text shown inside the dialog after a failed action, or `null`. */
  errorMessage?: string | null
  onConfirm: () => void
  onCancel: () => void
}>

const FOCUSABLE_SELECTOR = 'button:not([disabled]), [href], input:not([disabled]), [tabindex]:not([tabindex="-1"])'

/** Modal that asks an admin to confirm a destructive action. Esc cancels; Tab stays inside. */
export function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel,
  pendingLabel,
  isPending,
  errorMessage = null,
  onConfirm,
  onCancel,
}: ConfirmDialogProps): ReactElement {
  const panelRef = useRef<HTMLDivElement>(null)

  // Give focus back to the element that opened the dialog.
  useEffect(() => {
    if (!open) return undefined
    const trigger = document.activeElement instanceof HTMLElement ? document.activeElement : null
    return () => trigger?.focus()
  }, [open])

  const onKeyDown = (event: KeyboardEvent<HTMLDivElement>): void => {
    if (event.key === 'Escape') {
      event.stopPropagation()
      if (!isPending) onCancel()
      return
    }
    if (event.key !== 'Tab' || !panelRef.current) return

    const focusable = Array.from(panelRef.current.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR))
    if (focusable.length === 0) {
      event.preventDefault()
      return
    }
    const first = focusable[0]
    const last = focusable[focusable.length - 1]
    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault()
      last.focus()
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault()
      first.focus()
    }
  }

  return (
    <AnimatePresence>
      {open && (
        <motion.div
          key="confirm-backdrop"
          className="fixed inset-0 z-50 flex items-center justify-center bg-wb-ink/40 p-4"
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          transition={{ duration: 0.2 }}
          onClick={() => {
            if (!isPending) onCancel()
          }}
          onKeyDown={onKeyDown}
        >
          <motion.div
            ref={panelRef}
            role="dialog"
            aria-modal="true"
            aria-labelledby="confirm-dialog-title"
            aria-describedby="confirm-dialog-message"
            className="w-full max-w-md rounded-wb-card bg-wb-surface-card p-6 shadow-wb-overlay"
            initial={{ opacity: 0, y: 16 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: 16 }}
            transition={{ duration: 0.2 }}
            onClick={(event) => event.stopPropagation()}
          >
            <h2 id="confirm-dialog-title" className="text-xl font-bold text-wb-ink">
              {title}
            </h2>
            <p id="confirm-dialog-message" className="mt-3 text-wb-ink-muted">
              {message}
            </p>
            {errorMessage && (
              <p role="alert" className="mt-3 text-sm font-semibold text-wb-danger">
                {errorMessage}
              </p>
            )}
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
                {isPending ? pendingLabel : confirmLabel}
              </button>
            </div>
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  )
}
