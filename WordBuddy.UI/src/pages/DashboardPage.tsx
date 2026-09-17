import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { Link } from 'react-router-dom'
import { useAuthStore } from '../store/authStore'

export function DashboardPage(): ReactElement {
  const user = useAuthStore((state) => state.user)

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <h1 className="text-3xl font-extrabold text-sky-900">
        Hi, {user?.displayName ?? 'friend'}! 👋
      </h1>
      <p className="mt-2 text-lg text-sky-700">Ready to learn something new today?</p>

      <div className="mt-6 grid gap-4 sm:grid-cols-2">
        <Link
          to="/lessons"
          className="flex items-center gap-4 rounded-3xl bg-white p-6 shadow-md transition-transform hover:-translate-y-1 hover:shadow-lg"
        >
          <span className="text-4xl">📚</span>
          <div>
            <p className="text-xl font-bold text-sky-900">Browse Lessons</p>
            <p className="text-sky-600">Vocabulary, grammar, and daily phrases</p>
          </div>
        </Link>

        <Link
          to="/progress"
          className="flex items-center gap-4 rounded-3xl bg-white p-6 shadow-md transition-transform hover:-translate-y-1 hover:shadow-lg"
        >
          <span className="text-4xl">✅</span>
          <div>
            <p className="text-xl font-bold text-sky-900">My Progress</p>
            <p className="text-sky-600">See what you've completed so far</p>
          </div>
        </Link>
      </div>
    </motion.div>
  )
}
