import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useParams } from 'react-router-dom'
import { recordProgress } from '../api/progress'
import { getLessonDetail } from '../api/lessons'
import { DailyPhraseList } from '../components/lessons/DailyPhraseList'
import { GrammarRuleList } from '../components/lessons/GrammarRuleList'
import { VocabularyList } from '../components/lessons/VocabularyList'

export function LessonDetailPage(): ReactElement {
  const { id } = useParams<{ id: string }>()
  const queryClient = useQueryClient()

  const { data: lesson, isLoading, isError } = useQuery({
    queryKey: ['lesson', id],
    queryFn: () => getLessonDetail(id!),
    enabled: Boolean(id),
  })

  const completeMutation = useMutation({
    mutationFn: () => recordProgress({ lessonId: id!, isCompleted: true }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['progress'] }),
  })

  if (isLoading) {
    return <p className="text-lg text-wb-ink-muted">Loading lesson…</p>
  }

  if (isError || !lesson) {
    return <p className="text-lg text-wb-danger">Couldn't load this lesson right now.</p>
  }

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <h1 className="text-3xl font-extrabold text-wb-ink">{lesson.title}</h1>
      <p className="mt-1 text-lg text-wb-ink-muted">{lesson.description}</p>

      <div className="mt-6">
        {lesson.type === 'Vocabulary' && <VocabularyList items={lesson.vocabularyItems} />}
        {lesson.type === 'Grammar' && <GrammarRuleList rules={lesson.grammarRules} />}
        {lesson.type === 'DailyPhrase' && <DailyPhraseList phrases={lesson.dailyPhrases} />}
      </div>

      <button
        type="button"
        onClick={() => completeMutation.mutate()}
        disabled={completeMutation.isPending || completeMutation.isSuccess}
        className="mt-8 rounded-wb-md bg-wb-success px-6 py-3 text-lg font-bold text-wb-on-success shadow disabled:opacity-60"
      >
        {completeMutation.isSuccess ? 'Completed! 🎉' : completeMutation.isPending ? 'Saving…' : 'Mark as Complete'}
      </button>
    </motion.div>
  )
}
