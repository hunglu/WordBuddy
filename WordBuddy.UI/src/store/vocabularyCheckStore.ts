import { create } from 'zustand'
import { persist } from 'zustand/middleware'

interface VocabularyCheckState {
  /** Last word count the learner picked for a recall-check session — a client-owned UI
   * preference, not server data, so it lives in Zustand rather than TanStack Query's cache. */
  lastUsedCount: number
  setLastUsedCount: (count: number) => void
}

export const useVocabularyCheckStore = create<VocabularyCheckState>()(
  persist(
    (set) => ({
      lastUsedCount: 10,
      setLastUsedCount: (count) => set({ lastUsedCount: count }),
    }),
    { name: 'wordbuddy-vocabulary-check' },
  ),
)
