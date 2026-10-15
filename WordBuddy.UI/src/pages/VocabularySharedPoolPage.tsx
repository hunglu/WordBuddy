import { motion } from 'framer-motion'
import { useId, useState, type ReactElement } from 'react'
import { CARD_PAGE_SIZE } from '../api/paging'
import { Pager } from '../components/Pager'
import { PersonalContextField } from '../components/vocabulary/PersonalContextField'
import { usePagination } from '../hooks/usePagination'
import { useAddSharedWordToMyList, useSharedVocabularyWords } from '../hooks/useVocabulary'

type AddToMyListControlsProps = Readonly<{
  isPending: boolean
  onAdd: (personalContext?: string) => void
}>

/** "Add to My List" with an optional private note, revealed by "Add a note". */
function AddToMyListControls({ isPending, onAdd }: AddToMyListControlsProps): ReactElement {
  const [showNote, setShowNote] = useState(false)
  const [note, setNote] = useState('')
  const noteId = useId()

  return (
    <div className="mt-2 flex flex-col gap-2">
      {showNote && (
        <PersonalContextField id={`note-${noteId}`} value={note} onChange={(event) => setNote(event.target.value)} />
      )}
      <div className="flex gap-2">
        <button
          type="button"
          onClick={() => onAdd(note.trim() || undefined)}
          disabled={isPending}
          className="rounded-wb-md bg-wb-primary px-4 py-2 text-sm font-bold text-wb-on-primary shadow-wb-card hover:bg-wb-primary-hover disabled:opacity-60"
        >
          Add to My List
        </button>
        <button
          type="button"
          onClick={() => setShowNote((open) => !open)}
          className="rounded-wb-md bg-wb-primary-soft px-3 py-2 text-sm font-semibold text-wb-ink hover:bg-wb-primary-soft-hover"
        >
          {showNote ? 'Hide note' : 'Add a note'}
        </button>
      </div>
    </div>
  )
}

export function VocabularySharedPoolPage(): ReactElement {
  const pagination = usePagination(CARD_PAGE_SIZE)
  const { data: page, isLoading, isError } = useSharedVocabularyWords(pagination.params)
  pagination.clamp(page?.totalCount)
  const words = page?.items
  const addToMyList = useAddSharedWordToMyList()

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <h1 className="text-3xl font-extrabold text-wb-ink">Community Word Pool</h1>
      <p className="mt-2 text-wb-ink-muted">Words other learners have shared and a moderator has approved.</p>

      {isLoading && <p className="mt-8 text-lg text-wb-ink-muted">Loading the shared pool…</p>}
      {isError && <p className="mt-8 text-lg text-wb-danger">Couldn't load the shared pool right now.</p>}

      {addToMyList.isError && (
        <p role="alert" className="mt-4 text-sm text-wb-danger">
          Couldn't add this word right now. Please try again.
        </p>
      )}

      {page && page.totalCount === 0 && (
        <p className="mt-8 text-lg text-wb-ink-muted">No shared words yet — check back soon!</p>
      )}

      {words && words.length > 0 && (
        <div className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {words.map((word) => (
            <div key={word.id} className="flex flex-col gap-2 rounded-wb-card bg-wb-surface-card p-5 shadow-wb-card">
              <p className="text-xl font-bold text-wb-ink">{word.word}</p>
              <p className="text-wb-ink-muted">{word.definition}</p>
              {word.example && <p className="text-sm italic text-wb-ink-muted">"{word.example}"</p>}

              {word.isMine ? (
                <span className="mt-2 self-start rounded-wb-pill border border-wb-share-shared-border bg-wb-share-shared-bg px-3 py-1 text-xs font-semibold text-wb-share-shared-ink">
                  Your word
                </span>
              ) : (
                <AddToMyListControls
                  isPending={addToMyList.isPending}
                  onAdd={(personalContext) => addToMyList.mutate({ id: word.id, personalContext })}
                />
              )}
            </div>
          ))}
        </div>
      )}

      {page && (
        <Pager
          page={pagination.page}
          pageSize={pagination.pageSize}
          totalCount={page.totalCount}
          onPageChange={pagination.setPage}
          label="Shared pool pages"
          itemLabel="words"
        />
      )}
    </motion.div>
  )
}
