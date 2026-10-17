import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, Navigate } from 'react-router-dom'
import { Pager } from '../components/Pager'
import { cardClass, inputClass, softButtonClass } from '../components/support/supportUi'
import { useAdminLexemes } from '../hooks/useAdminLexemes'
import { usePagination } from '../hooks/usePagination'
import { useAuthStore } from '../store/authStore'
import { canManageContent } from '../utils/roles'
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

/** `/admin/lexemes` — search and page through the dictionary headwords. Admin or moderator. */
export function AdminLexemesPage(): ReactElement {
  const { t } = useTranslation('admin')
  const canManage: boolean = canManageContent(useAuthStore((state) => state.user))

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
    canManage,
  )
  pagination.clamp(lexemes.data?.totalCount)

  // Server-side, `ManageContent` already rejects a non-admin call — this is a UX guard.
  if (!canManage) {
    return <Navigate to="/" replace />
  }

  const data = lexemes.data

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <h1 className="text-3xl font-extrabold text-wb-ink">{t('lexemes.title')}</h1>
      <p className="mt-1 text-wb-ink-muted">{t('lexemes.intro')}</p>

      <div className="mt-6 flex flex-wrap items-end gap-3">
        <label className="min-w-64 flex-1 text-sm font-semibold text-wb-ink">
          {t('lexemes.search')}
          <input
            type="search"
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
            placeholder={t('lexemes.searchPlaceholder')}
            aria-describedby="lexeme-search-hint"
            className={`${inputClass} mt-1`}
          />
          <span id="lexeme-search-hint" className="mt-1 block text-xs font-normal text-wb-ink-muted">
            {t('lexemes.searchHint')}
          </span>
        </label>
        <label className="text-sm font-semibold text-wb-ink">
          {t('lexemes.partOfSpeech')}
          <select
            value={partOfSpeech}
            onChange={(e) => {
              setPartOfSpeech(e.target.value as PartOfSpeech | '')
              pagination.reset()
            }}
            className={`${inputClass} mt-1`}
          >
            <option value="">{t('lexemes.all')}</option>
            {PARTS_OF_SPEECH.map((p) => (
              <option key={p} value={p}>
                {t(`pos.${p}`)}
              </option>
            ))}
          </select>
        </label>
        <label className="text-sm font-semibold text-wb-ink">
          {t('lexemes.cefr')}
          <select
            value={cefrLevel}
            onChange={(e) => {
              setCefrLevel(e.target.value as CefrLevel | '')
              pagination.reset()
            }}
            className={`${inputClass} mt-1`}
          >
            <option value="">{t('lexemes.all')}</option>
            {CEFR_LEVELS.map((l) => (
              <option key={l} value={l}>
                {l}
              </option>
            ))}
          </select>
        </label>
      </div>

      {lexemes.isLoading && <p className="mt-8 text-lg text-wb-ink-muted">{t('lexemes.loading')}</p>}
      {lexemes.isError && <p className="mt-8 text-lg text-wb-danger">{t('lexemes.loadFailed')}</p>}
      {data && data.items.length === 0 && <p className="mt-8 text-lg text-wb-ink-muted">{t('lexemes.none')}</p>}

      {data && data.items.length > 0 && (
        <div className={`mt-6 overflow-x-auto ${cardClass}`}>
          <table className="w-full text-left">
            <thead>
              <tr className="text-sm text-wb-ink-muted">
                <th className="pb-2 pr-4 font-semibold">{t('lexemes.colWord')}</th>
                <th className="pb-2 pr-4 font-semibold">{t('lexemes.colPos')}</th>
                <th className="pb-2 pr-4 font-semibold">{t('lexemes.colIpa')}</th>
                <th className="pb-2 pr-4 font-semibold">{t('lexemes.colCefr')}</th>
                <th className="pb-2 pr-4 font-semibold">{t('lexemes.colRank')}</th>
                <th className="pb-2 pr-4 font-semibold">{t('lexemes.colAudio')}</th>
                <th className="pb-2 pr-4 font-semibold">{t('lexemes.colSenses')}</th>
                <th className="pb-2 font-semibold">{t('lexemes.colActions')}</th>
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
                  <td className="py-3 pr-4 text-wb-ink">{lexeme.partOfSpeech ? t(`pos.${lexeme.partOfSpeech}`) : '—'}</td>
                  <td className="py-3 pr-4 text-wb-ink">
                    {lexeme.ipaUk ?? '—'} / {lexeme.ipaUs ?? '—'}
                  </td>
                  <td className="py-3 pr-4 text-wb-ink">{lexeme.cefrLevel ?? '—'}</td>
                  <td className="py-3 pr-4 text-wb-ink">{lexeme.frequencyRank ?? '—'}</td>
                  <td className="py-3 pr-4 text-wb-ink">
                    <span title={lexeme.hasUkAudio ? t('lexemes.ukAudio') : t('lexemes.noUkAudio')}>🇬🇧 {lexeme.hasUkAudio ? '🔊' : '—'}</span>{' '}
                    <span title={lexeme.hasUsAudio ? t('lexemes.usAudio') : t('lexemes.noUsAudio')}>🇺🇸 {lexeme.hasUsAudio ? '🔊' : '—'}</span>
                  </td>
                  <td className="py-3 pr-4 text-wb-ink">
                    {lexeme.catalogSenseCount}
                    {lexeme.privateLearnerSenseCount > 0 && (
                      <span className="text-wb-ink-muted">{t('lexemes.privateCount', { count: lexeme.privateLearnerSenseCount })}</span>
                    )}
                  </td>
                  <td className="py-3">
                    <Link to={`/admin/lexemes/${lexeme.id}`} className={softButtonClass}>
                      {t('lexemes.edit')}
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
          label={t('lexemes.pages')}
          itemLabel={t('lexemes.items')}
        />
      )}
    </motion.div>
  )
}
