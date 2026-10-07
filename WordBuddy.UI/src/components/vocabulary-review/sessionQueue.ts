import type { ExerciseType, SenseReview, SessionItem, VocabularySkill, WordStatus } from '../../types'

/** Choice exercises need the correct word plus 3 distractors. */
export const CHOICE_OPTION_COUNT = 4

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

export interface PickedExercise {
  exerciseType: ExerciseType
  skill: VocabularySkill
}

const RECALL_ONLY_STATUSES: WordStatus[] = ['Review', 'Mastered', 'Leech']

/** First level for a word, from its server status. */
export function initialLevel(status: WordStatus): ExerciseLevel {
  return RECALL_ONLY_STATUSES.includes(status) ? 'Recall' : 'Recognition'
}

/**
 * Picks the exercise for a queue item. Pure and deterministic.
 *
 * | Level | Exercise |
 * | --- | --- |
 * | Recognition (`New`/`Learning`, no correct answer yet) | PictureChoice (image) → ListeningChoice (audio) → Typing |
 * | Recall (`Learning` after 1 correct) | ListeningChoice (audio) → Typing |
 * | `Review`, `Mastered`, `Leech` | Typing |
 *
 * Choice exercises need at least 4 senses in the session; otherwise Typing.
 */
export function pickExercise(item: QueueItem, sense: SenseReview, sessionSenseCount: number): PickedExercise {
  const typing: PickedExercise = { exerciseType: 'Typing', skill: 'Spelling' }
  if (RECALL_ONLY_STATUSES.includes(item.status)) {
    return typing
  }

  const canChoose = sessionSenseCount >= CHOICE_OPTION_COUNT
  if (!canChoose) {
    return typing
  }

  if (item.level === 'Recognition' && sense.imageUrl) {
    return { exerciseType: 'PictureChoice', skill: 'Meaning' }
  }
  if (sense.audioUrl) {
    return { exerciseType: 'ListeningChoice', skill: 'Listening' }
  }
  return typing
}

export interface SessionState {
  queue: QueueItem[]
  answered: number
  correct: number
  /** Set when the child time cap ends the session early. */
  stoppedByTimeCap: boolean
  nextKey: number
}

export type SessionAction = { type: 'answer'; isCorrect: boolean } | { type: 'stop' }

/**
 * Builds the starting queue: due items first, then new items. Items whose sense is missing from
 * the Content reply (hidden or deleted) are skipped and never posted.
 */
export function createSessionState(dueItems: SessionItem[], newItems: SessionItem[], senseIds: Set<string>): SessionState {
  const queue: QueueItem[] = [...dueItems, ...newItems]
    .filter((item) => senseIds.has(item.senseId))
    .map((item, index) => ({
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
 */
export function sessionReducer(state: SessionState, action: SessionAction): SessionState {
  if (action.type === 'stop') {
    return { ...state, queue: [], stoppedByTimeCap: true }
  }

  const [current, ...rest] = state.queue
  if (!current) {
    return state
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

/** Correct word plus up to 3 distractors from the other session senses, shuffled. */
export function buildChoiceOptions(sense: SenseReview, allSenses: SenseReview[]): SenseReview[] {
  const distractors = shuffle(allSenses.filter((s) => s.senseId !== sense.senseId && s.word !== sense.word)).slice(
    0,
    CHOICE_OPTION_COUNT - 1,
  )
  return shuffle([sense, ...distractors])
}

function shuffle<T>(items: T[]): T[] {
  const copy = [...items]
  for (let i = copy.length - 1; i > 0; i -= 1) {
    const j = Math.floor(Math.random() * (i + 1))
    ;[copy[i], copy[j]] = [copy[j], copy[i]]
  }
  return copy
}

/** Typing check until WB-28: trim + case-insensitive exact match. */
export function isTypedAnswerCorrect(typed: string, word: string): boolean {
  return typed.trim().toLowerCase() === word.trim().toLowerCase()
}

/** What an exercise reports when the learner answers. */
export interface ExerciseAnswer {
  isCorrect: boolean
  responseMs: number
  hintUsed: boolean
}
