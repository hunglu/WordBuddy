import { AnimatePresence, motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useCallback, useEffect, useReducer, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { problemCode } from '../api/supportLinks'
import { ListeningChoiceExercise } from '../components/vocabulary-review/ListeningChoiceExercise'
import { PersonalContextNote } from '../components/vocabulary-review/PersonalContextNote'
import { PictureChoiceExercise } from '../components/vocabulary-review/PictureChoiceExercise'
import { SessionSummary } from '../components/vocabulary-review/SessionSummary'
import { TypingExercise } from '../components/vocabulary-review/TypingExercise'
import {
  createSessionState,
  sessionReducer,
  type ExerciseAnswer,
} from '../components/vocabulary-review/sessionQueue'
import { useCreateExercise, useRecordReview, useVocabularySession } from '../hooks/useVocabularyReview'
import { useAuthStore } from '../store/authStore'
import type { ReviewResult, VocabularyExercise, VocabularySession } from '../types'

/** Child sessions: soft notice at 10 min, hard stop at 15 min. UI cap only; unanswered items stay due. */
const CHILD_NOTICE_MS = 10 * 60 * 1000
const CHILD_STOP_MS = 15 * 60 * 1000

/** Problem codes of "no exercise for this word": the word is skipped (hidden, removed or already done). */
const SKIP_CODES: ReadonlySet<string> = new Set([
  'Exercise.SenseUnavailable',
  'Exercise.SenseNotInSession',
  'Exercise.SenseCompleted',
  'Review.WordNotInList',
])

const PRIMARY_BUTTON =
  'rounded-wb-md bg-wb-primary px-6 py-3 text-lg font-bold text-wb-on-primary shadow-wb-card hover:bg-wb-primary-hover'

/** Daily vocabulary review: runs the SRS session with 3 exercise types. The server builds each exercise and checks each answer. */
export function VocabularyReviewPage(): ReactElement {
  const session = useVocabularySession()

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <h1 className="text-3xl font-extrabold text-wb-ink">Review</h1>
      <div className="mt-6">{renderBody()}</div>
    </motion.div>
  )

  function renderBody(): ReactElement {
    if (session.isError) {
      return (
        <div className="flex flex-col items-start gap-3">
          <p className="text-lg text-wb-danger">Couldn't load your review right now.</p>
          <button type="button" onClick={() => void session.refetch()} className={PRIMARY_BUTTON}>
            Try again
          </button>
        </div>
      )
    }

    if (session.isLoading || !session.data) {
      return <p className="text-lg text-wb-ink-muted">Getting today's words ready…</p>
    }

    if (session.data.dueItems.length + session.data.newItems.length === 0) {
      return (
        <div>
          <p className="text-lg text-wb-ink-muted">Nothing to review today. Add some words to your list!</p>
          <Link to="/vocabulary" className="mt-4 inline-block font-semibold text-wb-ink-muted hover:underline">
            Back to My Vocabulary
          </Link>
        </div>
      )
    }

    return <ReviewSession key={session.data.sessionId} session={session.data} />
  }
}

interface ReviewSessionProps {
  session: VocabularySession
}

function ReviewSession({ session }: ReviewSessionProps): ReactElement {
  const [state, dispatch] = useReducer(sessionReducer, undefined, () =>
    createSessionState(session.dueItems, session.newItems),
  )
  const isChild: boolean = useAuthStore((s) => s.user?.ageGroup === 'Child')
  const [nearlyDone, setNearlyDone] = useState(false)

  useEffect(() => {
    if (!isChild) {
      return undefined
    }
    const notice = window.setTimeout(() => setNearlyDone(true), CHILD_NOTICE_MS)
    const stop = window.setTimeout(() => dispatch({ type: 'stop' }), CHILD_STOP_MS)
    return () => {
      window.clearTimeout(notice)
      window.clearTimeout(stop)
    }
  }, [isChild])

  const handleNext = useCallback((isCorrect: boolean): void => dispatch({ type: 'answer', isCorrect }), [])
  const handleSkip = useCallback((): void => dispatch({ type: 'skip' }), [])

  const current = state.queue[0]
  if (!current) {
    return <SessionSummary answered={state.answered} correct={state.correct} stoppedByTimeCap={state.stoppedByTimeCap} />
  }

  return (
    <div className="flex flex-col items-center">
      <p className="mb-2 text-wb-ink-muted">
        {state.queue.length} left · {state.correct} correct
      </p>
      {nearlyDone && (
        <p data-testid="time-cap-notice" className="mb-4 font-semibold text-wb-highlight">
          Nearly done — a few more words!
        </p>
      )}

      <AnimatePresence mode="wait">
        <motion.div
          key={current.key}
          initial={{ opacity: 0, x: 40 }}
          animate={{ opacity: 1, x: 0 }}
          exit={{ opacity: 0, x: -40 }}
          transition={{ duration: 0.25 }}
          className="w-full max-w-md rounded-wb-card bg-wb-surface-card p-8 text-center shadow-wb-card"
        >
          <ExerciseCard
            sessionId={session.sessionId}
            senseId={current.senseId}
            onNext={handleNext}
            onSkip={handleSkip}
          />
        </motion.div>
      </AnimatePresence>
    </div>
  )
}

interface ExerciseCardProps {
  sessionId: string
  senseId: string
  onNext: (isCorrect: boolean) => void
  onSkip: () => void
}

/**
 * One presentation: asks the server for the exercise, renders it, sends the raw answer, and shows the
 * server's verdict. The browser never knows the right answer before the server replies.
 */
function ExerciseCard({ sessionId, senseId, onNext, onSkip }: ExerciseCardProps): ReactElement {
  const createExercise = useCreateExercise()
  const recordReview = useRecordReview()
  const requested = useRef(false)
  const [exercise, setExercise] = useState<VocabularyExercise | null>(null)
  const [loadFailed, setLoadFailed] = useState(false)
  const [submitted, setSubmitted] = useState<ExerciseAnswer | null>(null)
  const [result, setResult] = useState<ReviewResult | null>(null)
  const [saveFailed, setSaveFailed] = useState(false)

  const { mutate: createMutate } = createExercise
  const loadExercise = useCallback((): void => {
    setLoadFailed(false)
    createMutate(
      { sessionId, senseId },
      {
        onSuccess: (data) => setExercise(data),
        onError: (error) => {
          if (SKIP_CODES.has(problemCode(error) ?? '')) {
            onSkip()
          } else {
            setLoadFailed(true)
          }
        },
      },
    )
  }, [createMutate, sessionId, senseId, onSkip])

  useEffect(() => {
    if (requested.current) {
      return
    }
    requested.current = true
    loadExercise()
  }, [loadExercise])

  const { mutate: reviewMutate } = recordReview
  function send(answer: ExerciseAnswer): void {
    if (!exercise) {
      return
    }
    setSubmitted(answer)
    setSaveFailed(false)
    reviewMutate(
      { exerciseId: exercise.exerciseId, ...answer },
      {
        onSuccess: (data) => setResult(data),
        onError: () => setSaveFailed(true),
      },
    )
  }

  if (loadFailed) {
    return (
      <div className="flex flex-col items-center gap-3">
        <p className="text-wb-danger">Couldn't load this word.</p>
        <button type="button" onClick={loadExercise} className={PRIMARY_BUTTON}>
          Try again
        </button>
        <button type="button" onClick={onSkip} className="text-sm font-semibold text-wb-ink-muted hover:underline">
          Skip this word
        </button>
      </div>
    )
  }

  if (!exercise) {
    return <p className="text-wb-ink-muted">Getting your word ready…</p>
  }

  return (
    <>
      {exercise.exerciseType === 'PictureChoice' && <PictureChoiceExercise exercise={exercise} onAnswer={send} />}
      {exercise.exerciseType === 'ListeningChoice' && <ListeningChoiceExercise exercise={exercise} onAnswer={send} />}
      {exercise.exerciseType === 'Typing' && <TypingExercise exercise={exercise} onAnswer={send} />}

      <PersonalContextNote personalContext={exercise.prompt.personalContext} />

      {result && (
        <div data-testid="answer-feedback" className="mt-6 flex flex-col items-center gap-3">
          <p className={`text-lg font-bold ${result.isCorrect ? 'text-wb-success' : 'text-wb-danger'}`}>
            {result.isCorrect ? 'Correct!' : `The answer was "${result.correctAnswer}".`}
          </p>
          <button type="button" onClick={() => onNext(result.isCorrect)} className={PRIMARY_BUTTON}>
            Next
          </button>
        </div>
      )}

      {saveFailed && submitted && (
        <div data-testid="answer-save-failed" className="mt-6 flex flex-col items-center gap-3">
          <p className="text-sm text-wb-danger">Couldn't save this answer.</p>
          <button type="button" onClick={() => send(submitted)} className={PRIMARY_BUTTON}>
            Try again
          </button>
          <button type="button" onClick={onSkip} className="text-sm font-semibold text-wb-ink-muted hover:underline">
            Skip this word
          </button>
        </div>
      )}
    </>
  )
}
