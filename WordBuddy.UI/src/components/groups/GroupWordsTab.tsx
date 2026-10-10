import type { ReactElement } from 'react'
import { useState } from 'react'
import { problemMessage } from '../../api/supportLinks'
import { useAssignWordsToGroup, useGroupWordAssignments } from '../../hooks/useGroups'
import { useSharedVocabularyWords } from '../../hooks/useVocabulary'
import type { GroupWordAssignmentResult, LearnerGroupDetail } from '../../types'
import { cardClass, formatDate, inputClass, primaryButtonClass } from '../support/supportUi'
import { NO_ACCESS_MESSAGE, inlineErrorClass, isForbidden } from './groupUi'

/** Most words the server accepts in one assign call. */
const MAX_WORDS_PER_CALL = 50

type GroupWordsTabProps = Readonly<{ group: LearnerGroupDetail }>

/** Words tab: pick words from the shared pool, give them to every active member, and see the history. */
export function GroupWordsTab({ group }: GroupWordsTabProps): ReactElement {
  const pool = useSharedVocabularyWords()
  const history = useGroupWordAssignments(group.id)
  const assign = useAssignWordsToGroup()
  const [filter, setFilter] = useState<string>('')
  const [selected, setSelected] = useState<string[]>([])
  const [result, setResult] = useState<GroupWordAssignmentResult | null>(null)

  const activeCount = group.members.filter((member) => member.status === 'Active').length
  const needle = filter.trim().toLowerCase()
  const visibleWords = (pool.data ?? []).filter((word) => needle === '' || word.word.toLowerCase().includes(needle))

  function toggle(senseId: string): void {
    setSelected((current) =>
      current.includes(senseId) ? current.filter((id) => id !== senseId) : [...current, senseId],
    )
  }

  function submit(): void {
    assign.mutate(
      { groupId: group.id, senseIds: selected },
      {
        onSuccess: (counts) => {
          setResult(counts)
          setSelected([])
        },
      },
    )
  }

  return (
    <div className="flex flex-col gap-6">
      <section className={cardClass} aria-label="Assign words">
        <h2 className="text-xl font-bold text-wb-ink">Assign words</h2>
        <p className="text-sm text-wb-ink-muted">
          The words go on the list of every active member now. New members do not get earlier words. Words that are not
          cleared for children are skipped.
        </p>

        <label htmlFor="group-word-filter" className="mt-3 block text-sm font-semibold text-wb-ink">
          Search the shared pool
        </label>
        <input
          id="group-word-filter"
          type="search"
          value={filter}
          onChange={(event) => setFilter(event.target.value)}
          className={inputClass}
        />

        {pool.isLoading && <p className="mt-2 text-wb-ink-muted">Loading words…</p>}
        {pool.isError && <p className={`mt-2 ${inlineErrorClass}`}>Couldn't load the shared words right now.</p>}
        {pool.data && visibleWords.length === 0 && <p className="mt-2 text-wb-ink-muted">No words found.</p>}

        {visibleWords.length > 0 && (
          <ul className="mt-3 flex max-h-72 flex-col gap-2 overflow-y-auto">
            {visibleWords.map((word) => (
              <li key={word.id}>
                <label className="flex cursor-pointer items-start gap-2 text-wb-ink">
                  <input
                    type="checkbox"
                    className="mt-1"
                    checked={selected.includes(word.id)}
                    disabled={!selected.includes(word.id) && selected.length >= MAX_WORDS_PER_CALL}
                    onChange={() => toggle(word.id)}
                  />
                  <span>
                    <span className="font-semibold">{word.word}</span>
                    <span className="block text-sm text-wb-ink-muted">{word.definition}</span>
                  </span>
                </label>
              </li>
            ))}
          </ul>
        )}

        <button
          type="button"
          disabled={selected.length === 0 || activeCount === 0 || assign.isPending}
          onClick={submit}
          className={`${primaryButtonClass} mt-3`}
        >
          Assign {selected.length > 0 ? `${selected.length} word${selected.length === 1 ? '' : 's'}` : 'words'}
        </button>
        {activeCount === 0 && <p className="mt-2 text-sm text-wb-ink-muted">Add at least one active member first.</p>}

        {assign.isError && (
          <p className={`mt-2 ${inlineErrorClass}`}>
            {isForbidden(assign.error) ? NO_ACCESS_MESSAGE : problemMessage(assign.error, 'Could not assign the words.')}
          </p>
        )}
        {result && (
          <p className="mt-3 text-wb-ink" role="status">
            Added {result.added}, already had {result.alreadyHad}, skipped for children {result.skippedForChildren}.
          </p>
        )}
      </section>

      <section className={cardClass} aria-label="Assignment history">
        <h2 className="text-xl font-bold text-wb-ink">History</h2>
        {history.isLoading && <p className="mt-2 text-wb-ink-muted">Loading the history…</p>}
        {history.isError && (
          <p className={`mt-2 ${inlineErrorClass}`}>
            {isForbidden(history.error) ? NO_ACCESS_MESSAGE : "Couldn't load the history right now."}
          </p>
        )}
        {history.data && history.data.length === 0 && <p className="mt-2 text-wb-ink-muted">No words assigned yet.</p>}
        {history.data && history.data.length > 0 && (
          <ul className="mt-3 flex flex-col gap-2">
            {history.data.map((item) => (
              <li key={item.id} className="text-wb-ink">
                <span className="font-semibold">{item.word}</span>
                <span className="text-sm text-wb-ink-muted"> · {formatDate(item.assignedAtUtc)}</span>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  )
}
