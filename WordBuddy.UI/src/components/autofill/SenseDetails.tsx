import type { ReactElement } from 'react'
import type { PartOfSpeech, SenseTranslation } from '../../types'
import styles from './SenseCard.module.css'

/** The enrichment shown for one sense. All optional: older replies may omit them. */
export type SenseDetailsData = Readonly<{
  word: string
  definition: string
  partOfSpeech?: PartOfSpeech | null
  ipaUk?: string | null
  ipaUs?: string | null
  audioUkUrl?: string | null
  audioUsUrl?: string | null
  examples?: readonly string[] | null
  translations?: readonly SenseTranslation[] | null
  synonyms?: readonly string[] | null
  antonyms?: readonly string[] | null
  collocations?: readonly string[] | null
  topicTags?: readonly string[] | null
  registerNote?: string | null
  awaitingApproval?: boolean
}>

type SenseDetailsProps = Readonly<{ sense: SenseDetailsData }>

const LOCALE_LABEL: Record<string, string> = { vi: 'Vietnamese' }

/**
 * Word header (POS, IPA, UK/US audio) plus definition, examples, translations and enrichment lists.
 * When `awaitingApproval` is set (child, not approved yet) only the word and a "Waiting for approval"
 * badge are shown — never the definition.
 */
export function SenseDetails({ sense }: SenseDetailsProps): ReactElement {
  const play = (url: string): void => {
    void new Audio(url).play()
  }

  if (sense.awaitingApproval) {
    return (
      <>
        <div className={styles.header}>
          <p className={styles.word}>{sense.word}</p>
          {sense.partOfSpeech && <span className={styles.pos}>{sense.partOfSpeech}</span>}
        </div>
        <span className={styles.waiting}>Waiting for approval</span>
        <p className={styles.ipa}>A supporter will check this word first.</p>
      </>
    )
  }

  const examples = sense.examples ?? []
  const translations = sense.translations ?? []
  const lists: ReadonlyArray<readonly [string, readonly string[]]> = [
    ['Synonyms', sense.synonyms ?? []],
    ['Antonyms', sense.antonyms ?? []],
    ['Word partners', sense.collocations ?? []],
  ]

  return (
    <>
      <div className={styles.header}>
        <p className={styles.word}>{sense.word}</p>
        {sense.partOfSpeech && <span className={styles.pos}>{sense.partOfSpeech}</span>}
      </div>

      {(sense.ipaUk || sense.ipaUs || sense.audioUkUrl || sense.audioUsUrl) && (
        <div className={styles.header}>
          {sense.ipaUk && <span className={styles.ipa}>UK {sense.ipaUk}</span>}
          {sense.audioUkUrl && (
            <button type="button" className={styles.audioButton} onClick={() => play(sense.audioUkUrl ?? '')}>
              ▶ UK
            </button>
          )}
          {sense.ipaUs && <span className={styles.ipa}>US {sense.ipaUs}</span>}
          {sense.audioUsUrl && (
            <button type="button" className={styles.audioButton} onClick={() => play(sense.audioUsUrl ?? '')}>
              ▶ US
            </button>
          )}
        </div>
      )}

      <p className={styles.definition}>{sense.definition}</p>

      {examples.length > 0 && (
        <ul className={styles.examples}>
          {examples.map((example) => (
            <li key={example}>"{example}"</li>
          ))}
        </ul>
      )}

      {translations.map((t) => (
        <p key={t.locale} className={styles.translation}>
          {LOCALE_LABEL[t.locale] ?? t.locale}: {t.text}
        </p>
      ))}

      {sense.registerNote && <p className={styles.translation}>Note: {sense.registerNote}</p>}

      {lists
        .filter(([, items]) => items.length > 0)
        .map(([label, items]) => (
          <p key={label} className={styles.translation}>
            {label}: {items.join(', ')}
          </p>
        ))}

      {(sense.topicTags ?? []).length > 0 && (
        <div className={styles.tags}>
          {(sense.topicTags ?? []).map((tag) => (
            <span key={tag} className={styles.tag}>
              {tag}
            </span>
          ))}
        </div>
      )}
    </>
  )
}
