import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'

const MotionLink = motion.create(Link)

export function DashboardPage(): ReactElement {
  const { t } = useTranslation('dashboard')
  const user = useAuthStore((state) => state.user)

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <h1 className="text-3xl font-extrabold text-wb-ink">
        {t('home.greeting', { name: user?.displayName ?? t('home.friend') })}
      </h1>
      <p className="mt-2 text-lg text-wb-ink-muted">{t('home.ready')}</p>

      <div className="mt-6 grid gap-4 sm:grid-cols-2">
        <MotionLink
          whileHover={{ y: -4 }}
          whileTap={{ scale: 0.97 }}
          transition={{ duration: 0.15 }}
          to="/lessons"
          className="flex items-center gap-4 rounded-wb-card bg-wb-surface-card p-6 shadow-wb-card"
        >
          <span className="text-4xl">📚</span>
          <div>
            <p className="text-xl font-bold text-wb-ink">{t('home.lessonsTitle')}</p>
            <p className="text-wb-ink-muted">{t('home.lessonsHint')}</p>
          </div>
        </MotionLink>

        <MotionLink
          whileHover={{ y: -4 }}
          whileTap={{ scale: 0.97 }}
          transition={{ duration: 0.15 }}
          to="/progress"
          className="flex items-center gap-4 rounded-wb-card bg-wb-surface-card p-6 shadow-wb-card"
        >
          <span className="text-4xl">✅</span>
          <div>
            <p className="text-xl font-bold text-wb-ink">{t('home.progressTitle')}</p>
            <p className="text-wb-ink-muted">{t('home.progressHint')}</p>
          </div>
        </MotionLink>
      </div>
    </motion.div>
  )
}
