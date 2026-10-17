import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'
import { CHALLENGE_NO_CONSENT_CODE } from '../api/challenges'
import { problemCode, problemMessage } from '../api/supportLinks'
import { goalText } from '../components/challenges/challengeUi'
import { Pager } from '../components/Pager'
import { avatarEmoji, cardClass } from '../components/support/supportUi'
import { useChallengeBoard } from '../hooks/useChallenges'
import { usePagination } from '../hooks/usePagination'
import type { ChallengeBoard, ChallengeBoardEntry } from '../types'

/**
 * `/challenges/:challengeId` — the board. The server filters it: an adult gets the full ranking, a child
 * gets their own rank and the top 5 on one page (alias and avatar only). This page shows what it receives.
 */
export function ChallengeBoardPage(): ReactElement {
  const { t } = useTranslation('challenges')
  const { challengeId } = useParams<{ challengeId: string }>()
  const pagination = usePagination()
  const { data, isLoading, isError, error } = useChallengeBoard(challengeId, pagination.params)
  pagination.clamp(data?.entries.totalCount)

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <Link to="/challenges" className="text-sm font-semibold text-wb-primary">
        {t('board.back')}
      </Link>

      {isLoading && <p className="mt-6 text-lg text-wb-ink-muted">{t('board.loading')}</p>}
      {isError && (
        <p className="mt-6 text-lg text-wb-danger">
          {problemCode(error) === CHALLENGE_NO_CONSENT_CODE
            ? t('board.noConsent')
            : problemMessage(error, t('board.loadFailed'))}
        </p>
      )}

      {data && <Board board={data} onPageChange={pagination.setPage} />}
    </motion.div>
  )
}

type BoardProps = Readonly<{ board: ChallengeBoard; onPageChange: (page: number) => void }>

function Board({ board, onPageChange }: BoardProps): ReactElement {
  const { t } = useTranslation('challenges')
  const entries: ChallengeBoardEntry[] = board.entries.items
  // Only the child view has a score on the own row alone; the adult view has a score on every row.
  const showsScores = entries.every((entry) => entry.score !== null)

  return (
    <>
      <h1 className="mt-2 text-3xl font-extrabold text-wb-ink">{board.name}</h1>
      <p className="mt-1 text-wb-ink-muted">
        {t('board.summary', { status: t(`status.${board.status}`), goal: goalText(t, board), count: board.participantCount })}
      </p>
      {board.myRank !== null && (
        <p className="mt-2 text-lg font-bold text-wb-ink">
          {board.myScore !== null
            ? t('board.rankScore', { rank: board.myRank, score: board.myScore })
            : t('board.rank', { rank: board.myRank })}
        </p>
      )}

      {board.entries.totalCount === 0 ? (
        <p className="mt-6 text-wb-ink-muted">{t('board.nobody')}</p>
      ) : showsScores ? (
        <ScoreTable entries={entries} />
      ) : (
        <RankList entries={entries} />
      )}

      <div className="max-w-2xl">
        <Pager
          page={board.entries.page}
          pageSize={board.entries.pageSize}
          totalCount={board.entries.totalCount}
          onPageChange={onPageChange}
          label={t('board.pages')}
          itemLabel={t('board.items')}
        />
      </div>
    </>
  )
}

type EntriesProps = Readonly<{ entries: ChallengeBoardEntry[] }>

function ScoreTable({ entries }: EntriesProps): ReactElement {
  const { t } = useTranslation('challenges')
  return (
    <div className={`mt-6 max-w-2xl overflow-x-auto ${cardClass}`}>
      <table className="w-full text-left">
        <thead>
          <tr className="text-sm text-wb-ink-muted">
            <th className="py-2 pr-3">{t('board.colRank')}</th>
            <th className="py-2 pr-3">{t('board.colLearner')}</th>
            <th className="py-2 text-right">{t('board.colScore')}</th>
          </tr>
        </thead>
        <tbody>
          {entries.map((entry, index) => (
            <tr key={`${entry.rank ?? 'x'}-${index}`} className={entry.isMe ? 'bg-wb-primary-soft font-bold' : ''}>
              <td className="py-2 pr-3 text-wb-ink">{entry.rank ?? '–'}</td>
              <td className="py-2 pr-3 text-wb-ink">
                {avatarEmoji(entry.avatarId)} {entry.name}
                {entry.isMe ? t('board.you') : ''}
              </td>
              <td className="py-2 text-right text-wb-ink">
                {entry.score}
                {entry.targetReached === true ? ' ✅' : ''}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function RankList({ entries }: EntriesProps): ReactElement {
  const { t } = useTranslation('challenges')
  return (
    <ol className="mt-6 flex max-w-xl flex-col gap-2">
      {entries.map((entry, index) => (
        <li
          key={`${entry.rank ?? 'x'}-${index}`}
          className={`flex items-center gap-3 rounded-wb-card p-4 shadow-wb-card ${
            entry.isMe ? 'bg-wb-primary-soft' : 'bg-wb-surface-card'
          }`}
        >
          <span className="w-8 text-2xl font-extrabold text-wb-ink">{entry.rank ?? '–'}</span>
          <span className="text-3xl">{avatarEmoji(entry.avatarId)}</span>
          <span className="text-lg font-bold text-wb-ink">
            {entry.name}
            {entry.isMe ? t('board.you') : ''}
          </span>
        </li>
      ))}
    </ol>
  )
}
