import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { problemMessage } from '../api/supportLinks'
import { ChallengeCard } from '../components/challenges/ChallengeCard'
import { cardClass } from '../components/support/supportUi'
import { useChallenges } from '../hooks/useChallenges'
import { useAuthStore } from '../store/authStore'

/** `/challenges` — the open and running vocabulary challenges of the learner's audience. */
export function ChallengesPage(): ReactElement {
  const user = useAuthStore((state) => state.user)
  const isChild = user?.ageGroup === 'Child'
  const { data, isLoading, isError, error } = useChallenges()

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <h1 className="text-3xl font-extrabold text-wb-ink">Challenges</h1>
      <p className="mt-2 text-wb-ink-muted">
        Join a challenge and review your words. Only correct reviews of words that are due earn points.
      </p>

      {isLoading && <p className="mt-6 text-lg text-wb-ink-muted">Loading challenges…</p>}
      {isError && (
        <p className="mt-6 text-lg text-wb-danger">{problemMessage(error, "Couldn't load challenges right now.")}</p>
      )}

      {data && data.length === 0 && (
        <div className={`mt-6 max-w-xl ${cardClass}`}>
          <p className="text-4xl">{isChild ? '🤝' : '🏆'}</p>
          {isChild ? (
            <>
              <p className="mt-2 text-lg font-bold text-wb-ink">Ask your guardian</p>
              <p className="mt-1 text-wb-ink-muted">
                A guardian must allow challenges for you first. Then your challenges show up here.
              </p>
            </>
          ) : (
            <>
              <p className="mt-2 text-lg font-bold text-wb-ink">No challenges right now</p>
              <p className="mt-1 text-wb-ink-muted">New challenges start every week. Check back soon.</p>
            </>
          )}
        </div>
      )}

      {data && data.length > 0 && (
        <div className="mt-6 flex max-w-2xl flex-col gap-3">
          {data.map((challenge) => (
            <ChallengeCard key={challenge.id} challenge={challenge} />
          ))}
        </div>
      )}
    </motion.div>
  )
}
