import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { Link, useParams } from 'react-router-dom'
import { CHALLENGE_NO_CONSENT_CODE } from '../api/challenges'
import { problemCode, problemMessage } from '../api/supportLinks'
import { goalText, statusLabel } from '../components/challenges/challengeUi'
import { avatarEmoji, cardClass } from '../components/support/supportUi'
import { useChallengeBoard } from '../hooks/useChallenges'
import type { ChallengeBoard, ChallengeBoardEntry } from '../types'

/**
 * `/challenges/:challengeId` — the board. The server filters it: an adult gets the full ranking, a child
 * gets their own rank and the top 5 (alias and avatar only). This page shows what it receives.
 */
export function ChallengeBoardPage(): ReactElement {
  const { challengeId } = useParams<{ challengeId: string }>()
  const { data, isLoading, isError, error } = useChallengeBoard(challengeId)

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <Link to="/challenges" className="text-sm font-semibold text-wb-primary">
        ← All challenges
      </Link>

      {isLoading && <p className="mt-6 text-lg text-wb-ink-muted">Loading the board…</p>}
      {isError && (
        <p className="mt-6 text-lg text-wb-danger">
          {problemCode(error) === CHALLENGE_NO_CONSENT_CODE
            ? 'Ask your guardian to allow challenges.'
            : problemMessage(error, "Couldn't load the board right now.")}
        </p>
      )}

      {data && <Board board={data} />}
    </motion.div>
  )
}

type BoardProps = Readonly<{ board: ChallengeBoard }>

function Board({ board }: BoardProps): ReactElement {
  // Only the child view has a score on the own row alone; the adult view has a score on every row.
  const showsScores = board.entries.every((entry) => entry.score !== null)

  return (
    <>
      <h1 className="mt-2 text-3xl font-extrabold text-wb-ink">{board.name}</h1>
      <p className="mt-1 text-wb-ink-muted">
        {statusLabel[board.status]} · {goalText(board)} · {board.participantCount} learners
      </p>
      {board.myRank !== null && (
        <p className="mt-2 text-lg font-bold text-wb-ink">
          Your rank: {board.myRank}
          {board.myScore !== null ? ` · score ${board.myScore}` : ''}
        </p>
      )}

      {board.entries.length === 0 ? (
        <p className="mt-6 text-wb-ink-muted">Nobody has joined yet.</p>
      ) : showsScores ? (
        <ScoreTable entries={board.entries} />
      ) : (
        <RankList entries={board.entries} />
      )}
    </>
  )
}

type EntriesProps = Readonly<{ entries: ChallengeBoardEntry[] }>

function ScoreTable({ entries }: EntriesProps): ReactElement {
  return (
    <div className={`mt-6 max-w-2xl overflow-x-auto ${cardClass}`}>
      <table className="w-full text-left">
        <thead>
          <tr className="text-sm text-wb-ink-muted">
            <th className="py-2 pr-3">Rank</th>
            <th className="py-2 pr-3">Learner</th>
            <th className="py-2 text-right">Score</th>
          </tr>
        </thead>
        <tbody>
          {entries.map((entry, index) => (
            <tr key={`${entry.rank ?? 'x'}-${index}`} className={entry.isMe ? 'bg-wb-primary-soft font-bold' : ''}>
              <td className="py-2 pr-3 text-wb-ink">{entry.rank ?? '–'}</td>
              <td className="py-2 pr-3 text-wb-ink">
                {avatarEmoji(entry.avatarId)} {entry.name}
                {entry.isMe ? ' (you)' : ''}
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
            {entry.isMe ? ' (you)' : ''}
          </span>
        </li>
      ))}
    </ol>
  )
}
