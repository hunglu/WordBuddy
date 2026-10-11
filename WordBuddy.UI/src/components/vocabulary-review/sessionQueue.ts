import type { ReviewAnswer, SessionItem, WordStatus } from '../../types'

/** A word is re-queued at most this many times after wrong answers, so a session always ends. */
export const MAX_WRONG_REQUEUES = 3

/** Exercise level inside a session: recognition first, then recall. */
export type ExerciseLevel = 'Recognition' | 'Recall'

/** One presentation waiting in the session queue. */
export interface QueueItem {
  /** Unique per presentation, used as React key and to remount the exercise. */
  key: string
  senseId: string
  status: WordStatus
  level: ExerciseLevel
  wrongCount: number
}

const RECALL_ONLY_STATUSES: WordStatus[] = ['Review', 'Mastered', 'Leech']

/** First level for a word, from its server status. */
export function initialLevel(status: WordStatus): ExerciseLevel {
  return RECALL_ONLY_STATUSES.includes(status) ? 'Recall' : 'Recognition'
}

export interface SessionState {
  queue: QueueItem[]
  answered: number
  correct: number
  /** Set when the child time cap ends the session early. */
  stoppedByTimeCap: boolean
  nextKey: number
}

/**
 * - `answer`: the server judged the answer; `isCorrect` is the server's verdict.
 * - `skip`: the server has no exercise for this word (hidden or removed); drop it without counting.
 * - `stop`: the child time cap ended the session.
 */
export type SessionAction = { type: 'answer'; isCorrect: boolean } | { type: 'skip' } | { type: 'stop' }

/**
 * Builds the starting queue: due items first, then new items. The server picks the exercise for each
 * item when it is shown; an item the server cannot build an exercise for is skipped then.
 */
export function createSessionState(dueItems: SessionItem[], newItems: SessionItem[]): SessionState {
  const queue: QueueItem[] = [...dueItems, ...newItems].map((item, index) => ({
    key: `${item.senseId}-${index}`,
    senseId: item.senseId,
    status: item.status,
    level: initialLevel(item.status),
    wrongCount: 0,
  }))

  return { queue, answered: 0, correct: 0, stoppedByTimeCap: false, nextKey: queue.length }
}

/**
 * Session queue reducer.
 * - Correct at Recognition → re-queued once at Recall (the word moves up).
 * - Correct at Recall → done.
 * - Wrong → re-queued at the end at the same level (up to {@link MAX_WRONG_REQUEUES} times).
 * - Skip → removed, not counted.
 */
export function sessionReducer(state: SessionState, action: SessionAction): SessionState {
  if (action.type === 'stop') {
    return { ...state, queue: [], stoppedByTimeCap: true }
  }

  const [current, ...rest] = state.queue
  if (!current) {
    return state
  }

  if (action.type === 'skip') {
    return { ...state, queue: rest }
  }

  const answered = state.answered + 1
  const correct = state.correct + (action.isCorrect ? 1 : 0)
  const key = `${current.senseId}-${state.nextKey}`

  if (action.isCorrect) {
    const queue: QueueItem[] =
      current.level === 'Recognition' ? [...rest, { ...current, key, level: 'Recall' }] : rest
    return { ...state, queue, answered, correct, nextKey: state.nextKey + 1 }
  }

  const queue: QueueItem[] =
    current.wrongCount < MAX_WRONG_REQUEUES ? [...rest, { ...current, key, wrongCount: current.wrongCount + 1 }] : rest
  return { ...state, queue, answered, correct, nextKey: state.nextKey + 1 }
}

/** What an exercise reports when the learner answers: the raw answer, never a verdict. */
export interface ExerciseAnswer {
  answer: ReviewAnswer
  clientResponseMs: number
  hintUsed: boolean
}
