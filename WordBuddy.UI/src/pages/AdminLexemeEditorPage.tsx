import { isAxiosError } from 'axios'
import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { Link, Navigate, useParams } from 'react-router-dom'
import { LexemeAudioPanel } from '../components/admin-lexemes/LexemeAudioPanel'
import { LexemeForm } from '../components/admin-lexemes/LexemeForm'
import { LexemeSensesPanel } from '../components/admin-lexemes/LexemeSensesPanel'
import { useAdminLexeme } from '../hooks/useAdminLexemes'
import { useAuthStore } from '../store/authStore'
import { canManageContent } from '../utils/roles'

/** `/admin/lexemes/:id` — correct the dictionary fields and audio of one lexeme. Admin or moderator. */
export function AdminLexemeEditorPage(): ReactElement {
  const { id } = useParams<{ id: string }>()
  const canManage: boolean = canManageContent(useAuthStore((state) => state.user))

  const lexeme = useAdminLexeme(id, canManage)

  // Server-side, `ManageContent` already rejects a non-admin call — this is a UX guard.
  if (!canManage) {
    return <Navigate to="/" replace />
  }

  const data = lexeme.data
  const notFound: boolean = lexeme.isError && isAxiosError(lexeme.error) && lexeme.error.response?.status === 404

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <Link to="/admin/lexemes" className="text-sm font-semibold text-wb-ink-muted hover:underline">
        ← All lexemes
      </Link>

      {lexeme.isLoading && <p className="mt-8 text-lg text-wb-ink-muted">Loading the lexeme…</p>}
      {notFound && (
        <div className="mt-8">
          <p className="text-lg font-semibold text-wb-ink">Lexeme not found</p>
          <Link to="/admin/lexemes" className="mt-2 inline-block text-sm font-semibold text-wb-ink-muted hover:underline">
            Back to the lexeme list
          </Link>
        </div>
      )}
      {lexeme.isError && !notFound && <p className="mt-8 text-lg text-wb-danger">Couldn't load this lexeme right now.</p>}

      {data && (
        <>
          <div className="mt-2 flex flex-wrap items-center gap-3">
            <h1 className="text-3xl font-extrabold text-wb-ink">{data.lemma}</h1>
            <span className="rounded-wb-pill border border-wb-border-subtle px-3 py-1 text-xs font-bold text-wb-ink">
              {data.partOfSpeech ?? 'No part of speech'}
            </span>
          </div>
          <LexemeForm lexeme={data} />
          <LexemeAudioPanel lexeme={data} />
          <LexemeSensesPanel senses={data.senses} privateLearnerSenseCount={data.privateLearnerSenseCount} />
        </>
      )}
    </motion.div>
  )
}
