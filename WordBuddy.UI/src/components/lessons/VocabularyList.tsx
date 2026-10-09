import type { ReactElement } from 'react'
import type { VocabularyItem } from '../../types'

export function VocabularyList({ items }: { items: VocabularyItem[] }): ReactElement {
  return (
    <ul className="flex flex-col gap-3">
      {items.map((item) => (
        <li key={item.id} className="rounded-wb-lg bg-wb-surface-page p-4">
          <div className="flex items-center gap-3">
            <p className="text-xl font-bold text-wb-ink">{item.word}</p>
            {item.audio && (
              <button
                type="button"
                aria-label={`Play pronunciation of ${item.word}`}
                onClick={() => new Audio(item.audio!.url).play()}
                className="flex h-9 w-9 items-center justify-center rounded-wb-pill bg-wb-primary text-wb-on-primary hover:bg-wb-primary-hover"
              >
                🔊
              </button>
            )}
          </div>
          {item.awaitingApproval ? (
            <p className="mt-1 text-sm font-semibold text-wb-ink-muted">Waiting for approval</p>
          ) : (
            <>
              <p className="mt-1 text-wb-ink">{item.definition}</p>
              <p className="mt-1 text-sm italic text-wb-ink-muted">"{item.example}"</p>
            </>
          )}
        </li>
      ))}
    </ul>
  )
}
