import type { ReactElement, TextareaHTMLAttributes } from 'react'

export const PERSONAL_CONTEXT_MAX_LENGTH = 500

type PersonalContextFieldProps = Readonly<
  Omit<TextareaHTMLAttributes<HTMLTextAreaElement>, 'id' | 'value'> & {
    id: string
    /** Current text, for the character counter. */
    value: string
    error?: string
  }
>

/** Optional private note: "Where did you meet this word?". Max 500 characters. */
export function PersonalContextField({ id, value, error, ...rest }: PersonalContextFieldProps): ReactElement {
  return (
    <div>
      <label htmlFor={id} className="mb-1 block text-sm font-semibold text-wb-ink">
        Where did you meet this word? (optional)
      </label>
      <textarea
        id={id}
        rows={2}
        maxLength={PERSONAL_CONTEXT_MAX_LENGTH}
        aria-describedby={`${id}-hint`}
        className="w-full rounded-wb-md border-2 border-wb-border-control px-4 py-3 text-base focus:border-wb-primary"
        {...rest}
      />
      <div id={`${id}-hint`} className="mt-1 flex justify-between text-xs text-wb-ink-muted">
        <span>Only you can see this note.</span>
        <span>
          {value.length}/{PERSONAL_CONTEXT_MAX_LENGTH}
        </span>
      </div>
      {error && <p className="mt-1 text-sm text-wb-danger">{error}</p>}
    </div>
  )
}
