import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useEffect, useState } from 'react'
import { Link, Navigate } from 'react-router-dom'
import { Pager } from '../components/Pager'
import { cardClass, inputClass, softButtonClass } from '../components/support/supportUi'
import { useAdminLexemes } from '../hooks/useAdminLexemes'
import { usePagination } from '../hooks/usePagination'
import { useAuthStore } from '../store/authStore'
import type { CefrLevel, PartOfSpeech } from '../types'

const SEARCH_DEBOUNCE_MS = 300

const PARTS_OF_SPEECH: PartOfSpeech[] = [
  'Noun',
  'Verb',
  'Adjective',
  'Adverb',
  'Pronoun',
  'Preposition',
  'Conjunction',
  'Determiner',
  'Interjection',
  'Phrase',
]
const CEFR_LEVELS: CefrLevel[] = ['A1', 'A2', 'B1', 'B2', 'C1', 'C2']

/** `/admin/lexemes` — search and page through the dictionary headwords. Admin only. */
export function AdminLexemesPage(): ReactElement {
  const isAdmin: boolean = useAuthStore((state) => state.user?.isAdmin) === true

  const [searchText, setSearchText] = useState('')
  const [search, setSearch] = useState('')
  const [partOfSpeech, setPartOfSpeech] = useState<PartOfSpeech | ''>('')
  const [cefrLevel, setCefrLevel] = useState<CefrLevel | ''>('')
  const pagination = usePagination()
  const resetPage = pagination.reset

  // Wait for a pause in typing before asking the server; a new search starts at page 1.
  useEffect(() => {
    const timer = setTimeout(() => {
      setSearch(searchText.trim())
      resetPage()
    }, SEARCH_DEBOUNCE_MS)
    return () => clearTimeout(timer)
  }, [searchText, resetPage])

  const lexemes = useAdminLexemes(
    {
      search: search === '' ? undefined : search,
      partOfSpeech: partOfSpeech === '' ? undefined : partOfSpeech,
      cefrLevel: cefrLevel === '' ? undefined : cefrLevel,
      ...pagination.params,
    },
    isAdmin,
  )
  pagination.clamp(lexemes.data?.totalCount)

  // Server-side, `AdminOnly` already rejects a non-admin call — this is a UX guard.
  if (!isAdmin) {
    return <Navigate to="/" replace />
  }

  const data = lexemes.data

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <h1 className="text-3xl font-extrabold text-wb-ink">Lexemes</h1>
      <p className="mt-1 text-wb-ink-muted">Dictionary headwords: fix spelling data, part of speech, level and pronunciation audio.</p>

      <div className="mt-6 flex flex-wrap items-end gap-3">
        <label className="min-w-64 flex-1 text-sm font-semibold text-wb-ink">
          Search
          <input
            type="search"
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
            placeholder="Search words"
            aria-describedby="lexeme-search-hint"
            className={`${inputClass} mt-1`}
          />
          <span id="lexeme-search-hint" className="mt-1 block text-xs font-normal text-wb-ink-muted">
            Matches any part of the word. Words that start with your text come first.
          </span>
        </label>
        <label className="text-sm font-semibold text-wb-ink">
          Part of speech
          <select
            value={partOfSpeech}
            onChange={(e) => {
              setPartOfSpeech(e.target.value as PartOfSpeech | '')
              pagination.reset()
            }}
            className={`${inputClass} mt-1`}
          >
            <option value="">All</option>
            {PARTS_OF_SPEECH.map((p) => (
              <option key={p} value={p}>
                {p}
              </option>
            ))}
          </select>
        </label>
        <label className="text-sm font-semibold text-wb-ink">
          CEFR level
          <select
            value={cefrLevel}
            onChange={(e) => {
              setCefrLevel(e.target.value as CefrLevel | '')
              pagination.reset()
            }}
            className={`${inputClass} mt-1`}
          >
            <option value="">All</option>
            {CEFR_LEVELS.map((l) => (
              <option key={l} value={l}>
                {l}
              </option>
            ))}
          </select>
        </label>
      </div>

      {lexemes.isLoading && <p className="mt-8 text-lg text-wb-ink-muted">Loading lexemes…</p>}
      {lexemes.isError && <p className="mt-8 text-lg text-wb-danger">Couldn't load lexemes right now.</p>}
      {data && data.items.length === 0 && <p className="mt-8 text-lg text-wb-ink-muted">No lexemes match your search.</p>}

      {data && data.items.length > 0 && (
        <div className={`mt-6 overflow-x-auto ${cardClass}`}>
          <table className="w-full text-left">
            <thead>
              <tr className="text-sm text-wb-ink-muted">
                <th className="pb-2 pr-4 font-semibold">Word</th>
                <th className="pb-2 pr-4 font-semibold">Part of speech</th>
                <th className="pb-2 pr-4 font-semibold">IPA (UK / US)</th>
                <th className="pb-2 pr-4 font-semibold">CEFR</th>
                <th className="pb-2 pr-4 font-semibold">Rank</th>
                <th className="pb-2 pr-4 font-semibold">Audio</th>
                <th className="pb-2 pr-4 font-semibold">Senses</th>
                <th className="pb-2 font-semibold">Actions</th>
              </tr>
            </thead>
            <tbody>
              {data.items.map((lexeme) => (
                <tr key={lexeme.id} className="border-t border-wb-border-subtle">
                  <td className="py-3 pr-4 font-bold text-wb-ink">
                    <Link to={`/admin/lexemes/${lexeme.id}`} className="hover:underline">
                      {lexeme.lemma}
                    </Link>
                  </td>
                  <td className="py-3 pr-4 text-wb-ink">{lexeme.partOfSpeech ?? '—'}</td>
                  <td className="py-3 pr-4 text-wb-ink">
                    {lexeme.ipaUk ?? '—'} / {lexeme.ipaUs ?? '—'}
                  </td>
                  <td className="py-3 pr-4 text-wb-ink">{lexeme.cefrLevel ?? '—'}</td>
                  <td className="py-3 pr-4 text-wb-ink">{lexeme.frequencyRank ?? '—'}</td>
                  <td className="py-3 pr-4 text-wb-ink">
                    <span title={lexeme.hasUkAudio ? 'UK audio set' : 'No UK audio'}>🇬🇧 {lexeme.hasUkAudio ? '🔊' : '—'}</span>{' '}
                    <span title={lexeme.hasUsAudio ? 'US audio set' : 'No US audio'}>🇺🇸 {lexeme.hasUsAudio ? '🔊' : '—'}</span>
                  </td>
                  <td className="py-3 pr-4 text-wb-ink">
                    {lexeme.catalogSenseCount}
                    {lexeme.privateLearnerSenseCount > 0 && (
                      <span className="text-wb-ink-muted"> (+{lexeme.privateLearnerSenseCount} private)</span>
                    )}
                  </td>
                  <td className="py-3">
                    <Link to={`/admin/lexemes/${lexeme.id}`} className={softButtonClass}>
                      Edit
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {data && (
        <Pager
          page={pagination.page}
          pageSize={pagination.pageSize}
          totalCount={data.totalCount}
          onPageChange={pagination.setPage}
          label="Lexeme pages"
          itemLabel="lexemes"
        />
      )}
    </motion.div>
  )
}
