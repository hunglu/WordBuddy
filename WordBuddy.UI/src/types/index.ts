// Mirrors the backend's C# enums/DTOs. Union string literal types instead of TS `enum` —
// tsconfig.app.json sets `erasableSyntaxOnly: true`, which disallows real (non-erasable) enums.

export type AgeGroup = 'Child' | 'Adult'

export type Level = 'Beginner' | 'Intermediate' | 'Advanced'

export type LessonType = 'Vocabulary' | 'Grammar' | 'DailyPhrase'

export type MediaAssetType = 'Text' | 'Image' | 'Audio' | 'Video'

export type VocabularyShareStatus = 'Private' | 'PendingReview' | 'Shared' | 'Rejected'

// ── Identity ────────────────────────────────────────────────────────────────

export interface User {
  id: string
  email: string
  displayName: string
  ageGroup: AgeGroup
  isAdmin: boolean
}

export interface AuthToken {
  token: string
  expiresAtUtc: string
  user: User
}

// ── Content ─────────────────────────────────────────────────────────────────

export interface MediaAsset {
  id: string
  type: MediaAssetType
  url: string
}

export interface VocabularyItem {
  id: string
  word: string
  definition: string
  example: string
  audio?: MediaAsset
}

export interface GrammarRule {
  id: string
  title: string
  explanation: string
  examples: string[]
}

export interface DailyPhrase {
  id: string
  phrase: string
  translation: string
  audio?: MediaAsset
  video?: MediaAsset
}

export interface Lesson {
  id: string
  title: string
  description: string
  type: LessonType
  level: Level
  targetAgeGroup: AgeGroup
}

export interface LessonDetail extends Lesson {
  vocabularyItems: VocabularyItem[]
  grammarRules: GrammarRule[]
  dailyPhrases: DailyPhrase[]
}

export interface LessonFilters {
  type?: LessonType
  level?: Level
}

export interface PersonalVocabularyWord {
  id: string
  ownerUserId: string
  word: string
  definition: string
  example: string | null
  shareStatus: VocabularyShareStatus
  visibleToChildren: boolean
  createdAtUtc: string
}

/** A word as returned by the random-selection-for-check endpoint — same shape as
 * {@link PersonalVocabularyWord}, aliased for readability at call sites. */
export type VocabularyRecallCheckWord = PersonalVocabularyWord

// ── Progress ────────────────────────────────────────────────────────────────

export interface LearnerProgress {
  id: string
  lessonId: string
  lessonTitle: string
  isCompleted: boolean
  scorePercent: number | null
  completedAtUtc: string | null
}

export interface VocabularyRecallResultItem {
  vocabularyWordId: string
  word: string
  known: boolean
}

export interface VocabularyRecallSession {
  id: string
  checkedAtUtc: string
  wordsChecked: number
  wordsKnown: number
}

export interface VocabularyRecallProgress {
  totalWordsTracked: number
  knownCount: number
  learningCount: number
  recentSessions: VocabularyRecallSession[]
}
