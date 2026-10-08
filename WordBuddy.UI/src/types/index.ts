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
  /** Public alias (3-20 chars). Older persisted sessions may not have it. */
  alias?: string | null
  /** Avatar id from the fixed catalog. */
  avatarId?: string | null
  /** Whether the user has at least one active supporter (child learning gate). */
  hasActiveSupporter?: boolean
}

// ── Identity: support links (WB-24) ─────────────────────────────────────────

export type SupportLinkStatus = 'Invited' | 'PendingPrimaryApproval' | 'Active' | 'Revoked'

/** Display label only — no effect on permissions. */
export type SupportRelationship = 'Parent' | 'Teacher' | 'Partner' | 'Other'

export type InvitationSide = 'Learner' | 'Supporter'

export type LinkSide = 'Learner' | 'Supporter'

export type UnlinkRequestStatus =
  | 'Pending'
  | 'OverrideRequested'
  | 'Confirmed'
  | 'Declined'
  | 'Cancelled'
  | 'CompletedByAdmin'
  | 'RejectedByAdmin'

export interface UnlinkRequest {
  id: string
  status: UnlinkRequestStatus
  requestedByMe: boolean
  requestedAtUtc: string
  escalationAvailableAtUtc: string
}

export interface SupportLink {
  id: string
  learnerId: string
  learnerName: string
  learnerAvatarId: string | null
  supporterId: string
  supporterName: string
  supporterAvatarId: string | null
  isPrimary: boolean
  relationship: SupportRelationship | null
  status: SupportLinkStatus
  createdAtUtc: string
  unlinkRequest: UnlinkRequest | null
}

export interface SupportInvitation {
  id: string
  creatorSide: InvitationSide
  relationship: SupportRelationship | null
  expiresAtUtc: string
}

export interface CreatedInvitation {
  id: string
  code: string
  token: string
  expiresAtUtc: string
}

export interface MySupportLinks {
  asLearner: SupportLink[]
  asSupporter: SupportLink[]
  managedForChildren: SupportLink[]
  openInvitations: SupportInvitation[]
  hasActiveSupporter: boolean
}

export interface AdminUnlinkRequest {
  id: string
  linkId: string
  learnerId: string
  supporterId: string
  requestedById: string
  requestedBySide: LinkSide
  status: UnlinkRequestStatus
  requestedAtUtc: string
  escalatedAtUtc: string | null
}

export interface AdminSupportLink {
  id: string
  learnerId: string
  supporterId: string
  isPrimary: boolean
  relationship: SupportRelationship | null
  status: SupportLinkStatus
  createdAtUtc: string
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
  /** On "my words" / recall check: whether the caller wrote this word (only authors may share it).
   * Always `false` on the shared pool and moderation lists. */
  isAuthor: boolean
  /** On the shared pool only: whether the caller currently owns this word. `false` everywhere else,
   * and for words handed over to WordBuddy. */
  isMine: boolean
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

// ── Vocabulary review (SRS) ─────────────────────────────────────────────────

export type WordStatus = 'New' | 'Learning' | 'Review' | 'Mastered' | 'Leech'

export type ExerciseType = 'PictureChoice' | 'ListeningChoice' | 'Typing'

export type VocabularySkill = 'Meaning' | 'Listening' | 'Spelling' | 'Pronunciation' | 'Usage'

export type FsrsRating = 'Again' | 'Hard' | 'Good' | 'Easy'

/** One word in today's session (Progress). Word text comes from Content. */
export interface SessionItem {
  senseId: string
  status: WordStatus
  dueAtUtc: string
}

/** Today's session from `GET /api/progress/vocabulary/session`. */
export interface VocabularySession {
  sessionId: string
  dueItems: SessionItem[]
  newItems: SessionItem[]
  newWordCap: number
  newWordsIntroducedToday: number
}

/** A sense as needed by a review exercise, from `GET /api/vocabulary/senses?ids=`. */
export interface SenseReview {
  senseId: string
  word: string
  definition: string
  example: string | null
  audioUrl: string | null
  imageUrl: string | null
  personalContext: string | null
}

/** Body of `POST /api/progress/vocabulary/reviews`. */
export interface ReviewPayload {
  sessionId: string
  senseId: string
  exerciseType: ExerciseType
  skill: VocabularySkill
  isCorrect: boolean
  responseMs: number
  hintUsed: boolean
}

/** Outcome of one recorded answer. */
export interface ReviewResult {
  status: WordStatus
  dueAtUtc: string
  rating: FsrsRating
}
