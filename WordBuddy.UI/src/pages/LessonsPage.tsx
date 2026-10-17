import { useQuery } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { getLessons } from '../api/lessons'
import { levelClasses } from '../theme/variants'
import type { Level, LessonType } from '../types'

const TYPE_TABS: LessonType[] = ['Vocabulary', 'Grammar', 'DailyPhrase']

const LEVEL_PILLS: Level[] = ['Beginner', 'Intermediate', 'Advanced']

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
  const { t } = useTranslation('lessons')
  const [type, setType] = useState<LessonType | undefined>(undefined)
  const [level, setLevel] = useState<Level | undefined>(undefined)

  const { data, isLoading, isError } = useQuery({
    queryKey: ['lessons', type, level],
    queryFn: () => getLessons({ type, level }),
  })

  return (
    <div>
      <h1 className="text-3xl font-extrabold text-wb-ink">{t('title')}</h1>

      <div className="mt-4 flex flex-wrap gap-2">
        {[undefined, ...TYPE_TABS].map((tab) => (
          <button
            key={tab ?? 'all'}
            type="button"
            onClick={() => setType(tab)}
            className={`rounded-wb-pill px-5 py-2 text-base font-bold ${
              type === tab
                ? 'bg-wb-primary text-wb-on-primary shadow-wb-card'
                : 'bg-wb-surface-card text-wb-ink hover:bg-wb-hover-tint'
            }`}
          >
            {tab === undefined ? t('all') : t(`type.${tab}`)}
          </button>
        ))}
      </div>

      <div className="mt-3 flex flex-wrap gap-2">
        {[undefined, ...LEVEL_PILLS].map((pill) => (
          <button
            key={pill ?? 'all'}
            type="button"
            onClick={() => setLevel(pill)}
            className={`rounded-wb-pill border px-4 py-1.5 text-sm font-semibold ${
              level === pill
                ? 'border-wb-primary bg-wb-primary text-wb-on-primary'
                : 'border-wb-border-subtle bg-wb-surface-card text-wb-ink-muted hover:bg-wb-surface-page'
            }`}
          >
            {pill === undefined ? t('allLevels') : t(`level.${pill}`)}
          </button>
        ))}
      </div>

      {isLoading && <p className="mt-8 text-lg text-wb-ink-muted">{t('loading')}</p>}

      {isError && (
        <p className="mt-8 text-lg text-wb-danger">
          {t('loadFailed')}
        </p>
      )}

      {data && data.length === 0 && (
        <p className="mt-8 text-lg text-wb-ink-muted">{t('noMatch')}</p>
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
                  {t(`level.${lesson.level}`)}
                </span>
              </MotionLink>
            </motion.div>
          ))}
        </div>
      )}
    </div>
  )
}
