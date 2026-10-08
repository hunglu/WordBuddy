import { MutationCache, QueryCache, QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { AnimatePresence, MotionConfig } from 'framer-motion'
import type { ReactElement } from 'react'
import { Navigate, Route, Routes, useLocation } from 'react-router-dom'
import { BrowserRouter } from 'react-router-dom'
import { isSupporterRequiredError } from './api/supportLinks'
import { ProtectedRoute } from './components/ProtectedRoute'
import { ChildSupporterGate } from './components/support/ChildSupporterGate'
import { CURRENT_USER_KEY } from './hooks/useSupportLinks'
import { AppLayout } from './layouts/AppLayout'
import { PublicLayout } from './layouts/PublicLayout'
import { DashboardPage } from './pages/DashboardPage'
import { AcceptInvitationPage } from './pages/AcceptInvitationPage'
import { AdminSupportLinksPage } from './pages/AdminSupportLinksPage'
import { LessonDetailPage } from './pages/LessonDetailPage'
import { LessonsPage } from './pages/LessonsPage'
import { LoginPage } from './pages/LoginPage'
import { ProfilePage } from './pages/ProfilePage'
import { ProgressPage } from './pages/ProgressPage'
import { RegisterPage } from './pages/RegisterPage'
import { SupportLinksPage } from './pages/SupportLinksPage'
import { VocabularyBuilderPage } from './pages/VocabularyBuilderPage'
import { VocabularyReviewPage } from './pages/VocabularyReviewPage'
import { VocabularyModerationPage } from './pages/VocabularyModerationPage'
import { VocabularySharedPoolPage } from './pages/VocabularySharedPoolPage'

// A 403 Learner.SupporterRequired from Content/Progress (child without an active supporter)
// refetches the current user, so ChildSupporterGate switches to the "Add a supporter" screen.
function onApiError(error: unknown): void {
  if (isSupporterRequiredError(error)) {
    void queryClient.invalidateQueries({ queryKey: CURRENT_USER_KEY })
  }
}

const queryClient: QueryClient = new QueryClient({
  queryCache: new QueryCache({ onError: onApiError }),
  mutationCache: new MutationCache({ onError: onApiError }),
})

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
          <Route path="/lessons" element={<ChildSupporterGate><LessonsPage /></ChildSupporterGate>} />
          <Route path="/lessons/:id" element={<ChildSupporterGate><LessonDetailPage /></ChildSupporterGate>} />
          <Route path="/progress" element={<ProgressPage />} />
          <Route path="/vocabulary" element={<ChildSupporterGate><VocabularyBuilderPage /></ChildSupporterGate>} />
          <Route path="/vocabulary/review" element={<ChildSupporterGate><VocabularyReviewPage /></ChildSupporterGate>} />
          <Route path="/vocabulary/check" element={<Navigate to="/vocabulary/review" replace />} />
          <Route path="/vocabulary/shared" element={<ChildSupporterGate><VocabularySharedPoolPage /></ChildSupporterGate>} />
          <Route path="/vocabulary/moderation" element={<VocabularyModerationPage />} />
          <Route path="/support" element={<SupportLinksPage />} />
          <Route path="/support/accept/:token" element={<AcceptInvitationPage />} />
          <Route path="/admin/support-links" element={<AdminSupportLinksPage />} />
          <Route path="/profile" element={<ProfilePage />} />
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
