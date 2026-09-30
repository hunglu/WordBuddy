import type { ReactElement } from 'react'
import type { DailyPhrase } from '../../types'

export function DailyPhraseList({ phrases }: { phrases: DailyPhrase[] }): ReactElement {
  return (
    <ul className="flex flex-col gap-3">
      {phrases.map((phrase) => (
        <li key={phrase.id} className="rounded-wb-lg bg-wb-phrase-bg p-4">
          <p className="text-xl font-bold text-wb-phrase-ink">{phrase.phrase}</p>
          <p className="mt-1 text-wb-phrase-ink">{phrase.translation}</p>
          <div className="mt-2 flex gap-2">
            {phrase.audio && (
              <button
                type="button"
                aria-label="Play audio"
                onClick={() => new Audio(phrase.audio!.url).play()}
                className="flex h-9 w-9 items-center justify-center rounded-wb-pill bg-wb-secondary text-wb-on-secondary hover:bg-wb-secondary-hover"
              >
                🔊
              </button>
            )}
            {phrase.video && (
              <a
                href={phrase.video.url}
                target="_blank"
                rel="noreferrer"
                aria-label="Watch video"
                className="flex h-9 w-9 items-center justify-center rounded-wb-pill bg-wb-secondary text-wb-on-secondary hover:bg-wb-secondary-hover"
              >
                🎬
              </a>
            )}
          </div>
        </li>
      ))}
    </ul>
  )
}
