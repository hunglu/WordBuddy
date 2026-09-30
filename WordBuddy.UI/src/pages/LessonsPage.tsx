import { useQuery } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { getLessons } from '../api/lessons'
import { levelClasses } from '../theme/variants'
import type { Level, LessonType } from '../types'

const TYPE_TABS: { value: LessonType | undefined; label: string }[] = [
  { value: undefined, label: 'All' },
  { value: 'Vocabulary', label: 'Vocabulary' },
  { value: 'Grammar', label: 'Grammar' },
  { value: 'DailyPhrase', label: 'Daily Phrases' },
]

const LEVEL_PILLS: { value: Level | undefined; label: string }[] = [
  { value: undefined, label: 'All levels' },
  { value: 'Beginner', label: 'Beginner' },
  { value: 'Intermediate', label: 'Intermediate' },
  { value: 'Advanced', label: 'Advanced' },
]

const TYPE_COLORS: Record<LessonType, string> = {
  Vocabulary: 'bg-wb-vocab-bg text-wb-vocab-ink border-wb-vocab-border',
  Grammar: 'bg-wb-grammar-bg text-wb-grammar-ink border-wb-grammar-border',
  DailyPhrase: 'bg-wb-phrase-bg text-wb-phrase-ink border-wb-phrase-border',
}

const TYPE_ICONS: Record<LessonType, string> = {
  Vocabulary: '🔤',
  Grammar: '📐',
  DailyPhrase: '💬',
}

const MotionLink = motion.create(Link)

export function LessonsPage(): ReactElement {
  const [type, setType] = useState<LessonType | undefined>(undefined)
  const [level, setLevel] = useState<Level | undefined>(undefined)

  const { data, isLoading, isError } = useQuery({
    queryKey: ['lessons', type, level],
    queryFn: () => getLessons({ type, level }),
  })

  return (
    <div>
      <h1 className="text-3xl font-extrabold text-wb-ink">Lessons</h1>

      <div className="mt-4 flex flex-wrap gap-2">
        {TYPE_TABS.map((tab) => (
          <button
            key={tab.label}
            type="button"
            onClick={() => setType(tab.value)}
            className={`rounded-wb-pill px-5 py-2 text-base font-bold ${
              type === tab.value
                ? 'bg-wb-primary text-wb-on-primary shadow-wb-card'
                : 'bg-wb-surface-card text-wb-ink hover:bg-wb-hover-tint'
            }`}
          >
            {tab.label}
          </button>
        ))}
      </div>

      <div className="mt-3 flex flex-wrap gap-2">
        {LEVEL_PILLS.map((pill) => (
          <button
            key={pill.label}
            type="button"
            onClick={() => setLevel(pill.value)}
            className={`rounded-wb-pill border px-4 py-1.5 text-sm font-semibold ${
              level === pill.value
                ? 'border-wb-primary bg-wb-primary text-wb-on-primary'
                : 'border-wb-border-subtle bg-wb-surface-card text-wb-ink-muted hover:bg-wb-surface-page'
            }`}
          >
            {pill.label}
          </button>
        ))}
      </div>

      {isLoading && <p className="mt-8 text-lg text-wb-ink-muted">Loading lessons…</p>}

      {isError && (
        <p className="mt-8 text-lg text-wb-danger">
          Couldn't load lessons right now. Please try again in a moment.
        </p>
      )}

      {data && data.length === 0 && (
        <p className="mt-8 text-lg text-wb-ink-muted">No lessons match these filters yet.</p>
      )}

      {data && data.length > 0 && (
        <div className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {data.map((lesson, index) => (
            <motion.div
              key={lesson.id}
              initial={{ opacity: 0, y: 16 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.25, delay: index * 0.05 }}
            >
              <MotionLink
                to={`/lessons/${lesson.id}`}
                whileHover={{ y: -4 }}
                whileTap={{ scale: 0.97 }}
                transition={{ duration: 0.15 }}
                className={`block h-full rounded-wb-card border-2 p-5 shadow-wb-card ${TYPE_COLORS[lesson.type]}`}
              >
                <span className="text-3xl">{TYPE_ICONS[lesson.type]}</span>
                <p className="mt-2 text-xl font-bold">{lesson.title}</p>
                <p className="mt-1 text-sm opacity-80">{lesson.description}</p>
                <span className={`mt-3 inline-block rounded-wb-pill px-3 py-1 text-xs font-semibold ${levelClasses[lesson.level]}`}>
                  {lesson.level}
                </span>
              </MotionLink>
            </motion.div>
          ))}
        </div>
      )}
    </div>
  )
}
