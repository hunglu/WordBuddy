import type { ReactElement } from 'react'
import { useState } from 'react'
import { problemMessage } from '../../api/supportLinks'
import { useAttachLessonSense, useLessonSenseSearch } from '../../hooks/useAdminLessons'
import { shareStatusClasses } from '../../theme/variants'
import { inputClass, softButtonClass } from '../support/supportUi'

type AddWordPickerProps = Readonly<{
  lessonId: string
  /** Sense ids already in the lesson — their "Add" button is disabled. */
  attachedSenseIds: ReadonlySet<string>
}>

/** Search box + result list to add system and shared-pool words to a lesson. */
export function AddWordPicker({ lessonId, attachedSenseIds }: AddWordPickerProps): ReactElement {
  const [search, setSearch] = useState('')
  const results = useLessonSenseSearch(search.trim(), true)
  const attach = useAttachLessonSense()

  return (
    <div>
      <label htmlFor="add-word-search" className="block text-sm font-semibold text-wb-ink">
        Add a word
      </label>
      <input
        id="add-word-search"
        type="search"
        value={search}
        maxLength={100}
        placeholder="Search system and shared words…"
        onChange={(e) => setSearch(e.target.value)}
        className={`${inputClass} mt-1`}
      />

      {results.isLoading && <p className="mt-3 text-sm text-wb-ink-muted">Searching…</p>}
      {results.isError && <p className="mt-3 text-sm text-wb-danger">Couldn't search words right now.</p>}
      {attach.isError && (
        <p role="alert" className="mt-3 text-sm font-semibold text-wb-danger">
          {problemMessage(attach.error, 'Could not add that word.')}
        </p>
      )}
      {results.data && results.data.length === 0 && <p className="mt-3 text-sm text-wb-ink-muted">No matching words.</p>}

      <ul className="mt-3 flex max-h-80 flex-col gap-2 overflow-y-auto">
        {results.data?.map((sense) => {
          const attached = attachedSenseIds.has(sense.senseId)
          return (
            <li key={sense.senseId} className="flex items-center justify-between gap-3 rounded-wb-md bg-wb-surface-page p-3">
              <div className="min-w-0">
                <p className="font-bold text-wb-ink">
                  {sense.word}
                  <span className={`ml-2 rounded-wb-pill border px-2 py-0.5 text-xs font-semibold ${shareStatusClasses[sense.shareStatus]}`}>
                    {sense.source === 'System' ? 'System' : sense.shareStatus}
                  </span>
                  {!sense.visibleToChildren && (
                    <span className="ml-2 rounded-wb-pill bg-wb-danger-soft px-2 py-0.5 text-xs font-semibold text-wb-danger-soft-ink">
                      Hidden from children
                    </span>
                  )}
                </p>
                <p className="truncate text-sm text-wb-ink-muted">{sense.definition}</p>
              </div>
              <button
                type="button"
                disabled={attached || attach.isPending}
                onClick={() => attach.mutate({ lessonId, senseId: sense.senseId })}
                className={softButtonClass}
              >
                {attached ? 'Added' : 'Add'}
              </button>
            </li>
          )
        })}
      </ul>
    </div>
  )
}
