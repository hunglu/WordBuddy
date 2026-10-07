import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { AnimatePresence, MotionConfig } from 'framer-motion'
import type { ReactElement } from 'react'
import { Navigate, Route, Routes, useLocation } from 'react-router-dom'
import { BrowserRouter } from 'react-router-dom'
import { ProtectedRoute } from './components/ProtectedRoute'
import { AppLayout } from './layouts/AppLayout'
import { PublicLayout } from './layouts/PublicLayout'
import { DashboardPage } from './pages/DashboardPage'
import { LessonDetailPage } from './pages/LessonDetailPage'
import { LessonsPage } from './pages/LessonsPage'
import { LoginPage } from './pages/LoginPage'
import { ProgressPage } from './pages/ProgressPage'
import { RegisterPage } from './pages/RegisterPage'
import { VocabularyBuilderPage } from './pages/VocabularyBuilderPage'
import { VocabularyReviewPage } from './pages/VocabularyReviewPage'
import { VocabularyModerationPage } from './pages/VocabularyModerationPage'
import { VocabularySharedPoolPage } from './pages/VocabularySharedPoolPage'

const queryClient = new QueryClient()

function AnimatedRoutes(): ReactElement {
  const location = useLocation()

  return (
    <AnimatePresence mode="wait">
      <Routes location={location} key={location.pathname}>
        <Route element={<PublicLayout />}>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
        </Route>

        <Route
          element={
            <ProtectedRoute>
              <AppLayout />
            </ProtectedRoute>
          }
        >
          <Route path="/" element={<DashboardPage />} />
          <Route path="/lessons" element={<LessonsPage />} />
          <Route path="/lessons/:id" element={<LessonDetailPage />} />
          <Route path="/progress" element={<ProgressPage />} />
          <Route path="/vocabulary" element={<VocabularyBuilderPage />} />
          <Route path="/vocabulary/review" element={<VocabularyReviewPage />} />
          <Route path="/vocabulary/check" element={<Navigate to="/vocabulary/review" replace />} />
          <Route path="/vocabulary/shared" element={<VocabularySharedPoolPage />} />
          <Route path="/vocabulary/moderation" element={<VocabularyModerationPage />} />
        </Route>

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </AnimatePresence>
  )
}

function App(): ReactElement {
  return (
    <QueryClientProvider client={queryClient}>
      <MotionConfig reducedMotion="user">
        <BrowserRouter>
          <AnimatedRoutes />
        </BrowserRouter>
      </MotionConfig>
    </QueryClientProvider>
  )
}

export default App
