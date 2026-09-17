import type { ReactElement } from 'react'
import type { VocabularyItem } from '../../types'

export function VocabularyList({ items }: { items: VocabularyItem[] }): ReactElement {
  return (
    <ul className="flex flex-col gap-3">
      {items.map((item) => (
        <li key={item.id} className="rounded-2xl bg-sky-50 p-4">
          <div className="flex items-center gap-3">
            <p className="text-xl font-bold text-sky-900">{item.word}</p>
            {item.audio && (
              <button
                type="button"
                aria-label={`Play pronunciation of ${item.word}`}
                onClick={() => new Audio(item.audio!.url).play()}
                className="flex h-9 w-9 items-center justify-center rounded-full bg-sky-500 text-white hover:bg-sky-600"
              >
                🔊
              </button>
            )}
          </div>
          <p className="mt-1 text-sky-800">{item.definition}</p>
          <p className="mt-1 text-sm italic text-sky-600">"{item.example}"</p>
        </li>
      ))}
    </ul>
  )
}
