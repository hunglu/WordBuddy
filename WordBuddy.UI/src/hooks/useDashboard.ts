import { useQuery } from '@tanstack/react-query'
import { getLearnerDashboard, getMyDashboard } from '../api/progress'
import type { DashboardRange } from '../types'

/** The caller's own dashboard. Pass `enabled = false` when the page shows a learner instead. */
export function useMyDashboard(days: DashboardRange, enabled = true) {
  return useQuery({
    queryKey: ['dashboard', 'me', days],
    queryFn: () => getMyDashboard(days),
    enabled,
    retry: false,
  })
}

/** A learner's dashboard for an active supporter. Disabled without a learner id. */
export function useLearnerDashboard(learnerId: string | undefined, days: DashboardRange) {
  return useQuery({
    queryKey: ['dashboard', learnerId, days],
    queryFn: () => getLearnerDashboard(learnerId!, days), // runs only when `enabled` (learnerId is defined),
    enabled: learnerId !== undefined,
    retry: false,
  })
}
