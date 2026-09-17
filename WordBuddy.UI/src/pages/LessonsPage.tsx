import { useQuery } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { getLessons } from '../api/lessons'
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
  Vocabulary: 'bg-sky-100 text-sky-800 border-sky-300',
  Grammar: 'bg-emerald-100 text-emerald-800 border-emerald-300',
  DailyPhrase: 'bg-amber-100 text-amber-800 border-amber-300',
}

const TYPE_ICONS: Record<LessonType, string> = {
  Vocabulary: '🔤',
  Grammar: '📐',
  DailyPhrase: '💬',
}

export function LessonsPage(): ReactElement {
  const [type, setType] = useState<LessonType | undefined>(undefined)
  const [level, setLevel] = useState<Level | undefined>(undefined)

  const { data, isLoading, isError } = useQuery({
    queryKey: ['lessons', type, level],
    queryFn: () => getLessons({ type, level }),
  })

  return (
    <div>
      <h1 className="text-3xl font-extrabold text-sky-900">Lessons</h1>

      <div className="mt-4 flex flex-wrap gap-2">
        {TYPE_TABS.map((tab) => (
          <button
            key={tab.label}
            type="button"
            onClick={() => setType(tab.value)}
            className={`rounded-full px-5 py-2 text-base font-bold transition-colors ${
              type === tab.value
                ? 'bg-sky-500 text-white shadow'
                : 'bg-white text-sky-900 hover:bg-sky-100'
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
            className={`rounded-full border px-4 py-1.5 text-sm font-semibold transition-colors ${
              level === pill.value
                ? 'border-sky-500 bg-sky-500 text-white'
                : 'border-sky-200 bg-white text-sky-700 hover:bg-sky-50'
            }`}
          >
            {pill.label}
          </button>
        ))}
      </div>

      {isLoading && <p className="mt-8 text-lg text-sky-600">Loading lessons…</p>}

      {isError && (
        <p className="mt-8 text-lg text-rose-600">
          Couldn't load lessons right now. Please try again in a moment.
        </p>
      )}

      {data && data.length === 0 && (
        <p className="mt-8 text-lg text-sky-600">No lessons match these filters yet.</p>
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
              <Link
                to={`/lessons/${lesson.id}`}
                className={`block h-full rounded-3xl border-2 p-5 shadow-sm transition-transform hover:-translate-y-1 hover:shadow-md ${TYPE_COLORS[lesson.type]}`}
              >
                <span className="text-3xl">{TYPE_ICONS[lesson.type]}</span>
                <p className="mt-2 text-xl font-bold">{lesson.title}</p>
                <p className="mt-1 text-sm opacity-80">{lesson.description}</p>
                <span className="mt-3 inline-block rounded-full bg-white/60 px-3 py-1 text-xs font-semibold">
                  {lesson.level}
                </span>
              </Link>
            </motion.div>
          ))}
        </div>
      )}
    </div>
  )
}
