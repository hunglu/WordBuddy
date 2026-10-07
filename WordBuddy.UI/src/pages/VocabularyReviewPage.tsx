import { AnimatePresence, motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useEffect, useMemo, useReducer, useState } from 'react'
import { Link } from 'react-router-dom'
import { ListeningChoiceExercise } from '../components/vocabulary-review/ListeningChoiceExercise'
import { PersonalContextNote } from '../components/vocabulary-review/PersonalContextNote'
import { PictureChoiceExercise } from '../components/vocabulary-review/PictureChoiceExercise'
import { SessionSummary } from '../components/vocabulary-review/SessionSummary'
import { TypingExercise } from '../components/vocabulary-review/TypingExercise'
import {
  CHOICE_OPTION_COUNT,
  buildChoiceOptions,
  createSessionState,
  pickExercise,
  sessionReducer,
  type ExerciseAnswer,
  type PickedExercise,
} from '../components/vocabulary-review/sessionQueue'
import { useRecordReview, useSenses, useVocabularySession } from '../hooks/useVocabularyReview'
import { useAuthStore } from '../store/authStore'
import type { SenseReview, VocabularySession } from '../types'

/** Child sessions: soft notice at 10 min, hard stop at 15 min. UI cap only; unanswered items stay due. */
const CHILD_NOTICE_MS = 10 * 60 * 1000
const CHILD_STOP_MS = 15 * 60 * 1000

interface LastAnswer {
  isCorrect: boolean
  word: string
  saveFailed: boolean
}

/** Daily vocabulary review: runs the SRS session with 3 exercise types. No self-rating. */
export function VocabularyReviewPage(): ReactElement {
  const session = useVocabularySession()
  const ids: string[] = useMemo(
    () => (session.data ? [...session.data.dueItems, ...session.data.newItems].map((item) => item.senseId) : []),
    [session.data],
  )
  const senses = useSenses(ids)

  function retry(): void {
    void session.refetch()
    if (ids.length > 0) {
      void senses.refetch()
    }
  }

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <h1 className="text-3xl font-extrabold text-wb-ink">Review</h1>
      <div className="mt-6">{renderBody()}</div>
    </motion.div>
  )

  function renderBody(): ReactElement {
    if (session.isError || senses.isError) {
      return (
        <div className="flex flex-col items-start gap-3">
          <p className="text-lg text-wb-danger">Couldn't load your review right now.</p>
          <button
            type="button"
            onClick={retry}
            className="rounded-wb-md bg-wb-primary px-6 py-3 text-lg font-bold text-wb-on-primary shadow-wb-card hover:bg-wb-primary-hover"
          >
            Try again
          </button>
        </div>
      )
    }

    if (session.isLoading || (ids.length > 0 && senses.isLoading) || !session.data) {
      return <p className="text-lg text-wb-ink-muted">Getting today's words ready…</p>
    }

    const loadedSenses: SenseReview[] = senses.data ?? []
    if (loadedSenses.length === 0) {
      return (
        <div>
          <p className="text-lg text-wb-ink-muted">Nothing to review today. Add some words to your list!</p>
          <Link to="/vocabulary" className="mt-4 inline-block font-semibold text-wb-ink-muted hover:underline">
            Back to My Vocabulary
          </Link>
        </div>
      )
    }

    return <ReviewSession key={session.data.sessionId} session={session.data} senses={loadedSenses} />
  }
}

interface ReviewSessionProps {
  session: VocabularySession
  senses: SenseReview[]
}

function ReviewSession({ session, senses }: ReviewSessionProps): ReactElement {
  const senseById: Map<string, SenseReview> = useMemo(() => new Map(senses.map((s) => [s.senseId, s])), [senses])
  const [state, dispatch] = useReducer(sessionReducer, undefined, () =>
    createSessionState(session.dueItems, session.newItems, new Set(senseById.keys())),
  )
  const [lastAnswer, setLastAnswer] = useState<LastAnswer | null>(null)
  const recordReview = useRecordReview()
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

  const current = state.queue[0]
  const currentSense: SenseReview | undefined = current ? senseById.get(current.senseId) : undefined

  const currentKey: string | undefined = current?.key
  // Recomputed per presentation only, so options stay stable while the learner answers.
  const options: SenseReview[] = useMemo(
    () => (currentKey && currentSense ? buildChoiceOptions(currentSense, senses) : []),
    [currentKey, currentSense, senses],
  )

  if (!current || !currentSense) {
    return <SessionSummary answered={state.answered} correct={state.correct} stoppedByTimeCap={state.stoppedByTimeCap} />
  }

  const picked: PickedExercise = pickExercise(current, currentSense, senses.length)
  const exercise: PickedExercise =
    picked.exerciseType !== 'Typing' && options.length < CHOICE_OPTION_COUNT
      ? { exerciseType: 'Typing', skill: 'Spelling' }
      : picked

  function handleAnswer(answer: ExerciseAnswer): void {
    if (!currentSense) {
      return
    }
    const word: string = currentSense.word
    setLastAnswer({ isCorrect: answer.isCorrect, word, saveFailed: false })
    recordReview.mutate(
      {
        sessionId: session.sessionId,
        senseId: currentSense.senseId,
        exerciseType: exercise.exerciseType,
        skill: exercise.skill,
        isCorrect: answer.isCorrect,
        responseMs: answer.responseMs,
        hintUsed: answer.hintUsed,
      },
      { onError: () => setLastAnswer({ isCorrect: answer.isCorrect, word, saveFailed: true }) },
    )
  }

  function handleNext(): void {
    if (!lastAnswer) {
      return
    }
    dispatch({ type: 'answer', isCorrect: lastAnswer.isCorrect })
    setLastAnswer(null)
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
          {exercise.exerciseType === 'PictureChoice' && (
            <PictureChoiceExercise sense={currentSense} options={options} onAnswer={handleAnswer} />
          )}
          {exercise.exerciseType === 'ListeningChoice' && (
            <ListeningChoiceExercise sense={currentSense} options={options} onAnswer={handleAnswer} />
          )}
          {exercise.exerciseType === 'Typing' && <TypingExercise sense={currentSense} onAnswer={handleAnswer} />}

          <PersonalContextNote personalContext={currentSense.personalContext} />

          {lastAnswer && (
            <div data-testid="answer-feedback" className="mt-6 flex flex-col items-center gap-3">
              <p className={`text-lg font-bold ${lastAnswer.isCorrect ? 'text-wb-success' : 'text-wb-danger'}`}>
                {lastAnswer.isCorrect ? 'Correct!' : `The answer was "${lastAnswer.word}".`}
              </p>
              {lastAnswer.saveFailed && (
                <p className="text-sm text-wb-danger">Couldn't save this answer. It will come back next time.</p>
              )}
              <button
                type="button"
                onClick={handleNext}
                className="rounded-wb-md bg-wb-primary px-6 py-3 text-lg font-bold text-wb-on-primary shadow-wb-card hover:bg-wb-primary-hover"
              >
                Next
              </button>
            </div>
          )}
        </motion.div>
      </AnimatePresence>
    </div>
  )
}
