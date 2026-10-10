// Mirrors the backend's C# enums/DTOs. Union string literal types instead of TS `enum` —
// tsconfig.app.json sets `erasableSyntaxOnly: true`, which disallows real (non-erasable) enums.

export type AgeGroup = 'Child' | 'Adult'

export type Level = 'Beginner' | 'Intermediate' | 'Advanced'

export type LessonType = 'Vocabulary' | 'Grammar' | 'DailyPhrase'

export type MediaAssetType = 'Text' | 'Image' | 'Audio' | 'Video'

export type VocabularyShareStatus = 'Private' | 'PendingReview' | 'Shared' | 'Rejected'

/** Part of speech of a word (backend `PartOfSpeech`). */
export type PartOfSpeech =
  | 'Noun'
  | 'Verb'
  | 'Adjective'
  | 'Adverb'
  | 'Pronoun'
  | 'Preposition'
  | 'Conjunction'
  | 'Determiner'
  | 'Interjection'
  | 'Phrase'

/** How a word's content was made: by a person, or by the auto-fill (dictionary + Claude). */
export type SenseOrigin = 'Manual' | 'AutoFill'

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

export interface SenseTranslation {
  /** BCP-47 locale, for example `vi`. */
  locale: string
  text: string
}

/** WB-25 enrichment fields, added to every word DTO. Optional: older replies may omit them. */
export interface SenseEnrichment {
  partOfSpeech?: PartOfSpeech | null
  ipaUk?: string | null
  ipaUs?: string | null
  audioUkUrl?: string | null
  audioUsUrl?: string | null
  examples?: string[] | null
  translations?: SenseTranslation[] | null
  collocations?: string[] | null
  synonyms?: string[] | null
  antonyms?: string[] | null
  topicTags?: string[] | null
  registerNote?: string | null
  origin?: SenseOrigin
  /** Child callers only: an auto-filled word not approved yet. Then no content is sent. */
  awaitingApproval?: boolean
}

export interface VocabularyItem extends SenseEnrichment {
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

export interface PersonalVocabularyWord extends SenseEnrichment {
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

/** One auto-filled sense card. `awaitingApproval` (Child only): content fields are empty. */
export interface AutofillSense {
  senseId: string
  word: string
  definition: string
  partOfSpeech: PartOfSpeech | null
  ipaUk: string | null
  ipaUs: string | null
  audioUkUrl: string | null
  audioUsUrl: string | null
  examples: string[]
  translations: SenseTranslation[]
  collocations: string[]
  synonyms: string[]
  antonyms: string[]
  topicTags: string[]
  registerNote: string | null
  origin: SenseOrigin
  awaitingApproval: boolean
}

/** Reply of `GET /vocabulary/autofill`. `autofillUnavailable` → show the manual form. */
export interface AutofillResult {
  word: string
  autofillUnavailable: boolean
  senses: AutofillSense[]
}

/** An auto-filled word waiting for child approval. `learnerId` is null in the admin queue. */
export interface ChildApproval {
  senseId: string
  learnerId: string | null
  word: string
  definition: string
  partOfSpeech: PartOfSpeech | null
  examples: string[]
  translations: SenseTranslation[]
  /** Generator's hint for approvers; approval is still required. */
  childSuitableHint: boolean | null
  createdAtUtc: string
}

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

/** What the learner sees of an exercise. The server never sends the answer. */
export interface ExercisePrompt {
  definition: string
  /** Set for `PictureChoice` only. */
  imageUrl: string | null
  /** Set for `ListeningChoice` only. */
  audioUrl: string | null
  personalContext: string | null
  /** First letter of the word, for the `Typing` hint only. */
  hintFirstLetter: string | null
}

/** One choice option. `key` is opaque; send it back as the answer. */
export interface ExerciseOption {
  key: string
  text: string
}

/** An exercise issued by `POST /api/progress/vocabulary/exercises`. */
export interface VocabularyExercise {
  exerciseId: string
  exerciseType: ExerciseType
  skill: VocabularySkill
  prompt: ExercisePrompt
  /** Empty for `Typing`. */
  options: ExerciseOption[]
}

/** Body of `POST /api/progress/vocabulary/exercises`. */
export interface CreateExercisePayload {
  sessionId: string
  senseId: string
}

/** The learner's raw answer: a picked option key or typed text. The server checks it. */
export type ReviewAnswer = { optionKey: string } | { text: string }

/** Body of `POST /api/progress/vocabulary/reviews`. */
export interface ReviewPayload {
  exerciseId: string
  answer: ReviewAnswer
  clientResponseMs: number
  hintUsed: boolean
}

/** Outcome of one recorded answer. `isCorrect` and `correctAnswer` come from the server. */
export interface ReviewResult {
  status: WordStatus
  dueAtUtc: string
  rating: FsrsRating
  isCorrect: boolean
  correctAnswer: string
}

// ── Learner dashboard (WB-26) ───────────────────────────────────────────────
// Dates are `YYYY-MM-DD` (local day of the learner). No type here carries a time of day.

export type DashboardRange = 7 | 30 | 90

export interface DayActivity {
  date: string
  reviews: number
}

export interface WeekActivity {
  weekStart: string
  activeDays: number
}

export interface DailyGoal {
  date: string
  plannedItems: number
  answeredWords: number
  percent: number
}

export interface DashboardActivity {
  currentStreakDays: number
  activeDaysPerWeek: WeekActivity[]
  heatmap: DayActivity[]
  dailyGoals: DailyGoal[]
}

export interface SupporterWordsAdded {
  supporterId: string
  count: number
}

export interface WordsAdded {
  date: string
  self: number
  supporters: SupporterWordsAdded[]
}

export interface WordStatusCount {
  status: WordStatus
  count: number
}

export interface DashboardWords {
  addedPerDay: WordsAdded[]
  addedPerWeek: WordsAdded[]
  perStatus: WordStatusCount[]
  reviewsPerDay: DayActivity[]
}

export interface SkillRetention {
  skill: VocabularySkill
  retention: number | null
  sample: number
}

export interface DashboardRetention {
  overall: number | null
  sample: number
  bySkill: SkillRetention[]
}

export interface LeechWord {
  senseId: string
  lapses: number
}

export interface SlowWord {
  senseId: string
  medianResponseMs: number
  answers: number
}

export interface DashboardStruggle {
  leeches: LeechWord[]
  weakestSkill: VocabularySkill | null
  slowestWords: SlowWord[]
}

export interface DashboardGamingSignals {
  totalAnswers: number
  quickWrongCount: number
  quickWrongPercent: number
  hintPercent: number
  hintRateFlagged: boolean
  unfinishedSessions: number
}

/** From `GET /api/progress/dashboard/me` and `.../learners/{learnerId}`. */
export interface LearnerDashboard {
  learnerId: string
  days: number
  from: string
  to: string
  activity: DashboardActivity
  words: DashboardWords
  retention: DashboardRetention
  struggle: DashboardStruggle
  gaming: DashboardGamingSignals
}

// ── Learning groups (WB-27) ─────────────────────────────────────────────────

export type GroupMemberStatus = 'PendingPrimaryApproval' | 'Active' | 'Removed'

/** A member as the group owner sees it: public name and avatar only (a child without alias is "Child learner"). */
export interface GroupMember {
  memberId: string
  learnerId: string
  publicName: string
  avatarId: string | null
  ageGroup: AgeGroup
  status: GroupMemberStatus
  addedAtUtc: string
}

/** From `GET /api/auth/groups/{id}` (owner only). */
export interface LearnerGroupDetail {
  id: string
  ownerId: string
  name: string
  createdAtUtc: string
  members: GroupMember[]
}

export interface OwnedGroup {
  id: string
  name: string
  activeMemberCount: number
  pendingMemberCount: number
  createdAtUtc: string
}

/** A group the caller belongs to: name and owner only, never other members. */
export interface JoinedGroup {
  id: string
  name: string
  ownerName: string
  ownerAvatarId: string | null
}

export interface MyGroups {
  owned: OwnedGroup[]
  joined: JoinedGroup[]
}

export type AddMemberOutcome = 'Added' | 'PendingApproval' | 'Rejected'

export interface AddMemberResult {
  learnerId: string
  outcome: AddMemberOutcome
  /** Set when `outcome` is `Rejected`. */
  errorCode: string | null
}

/** A pending child membership waiting for the caller (as Primary supporter). */
export interface PendingGroupApproval {
  memberId: string
  groupId: string
  groupName: string
  ownerName: string
  learnerId: string
  learnerName: string
  learnerAvatarId: string | null
  requestedAtUtc: string
}

export interface GroupWordAssignmentResult {
  added: number
  alreadyHad: number
  skippedForChildren: number
}

export interface GroupWordAssignment {
  id: string
  senseId: string
  word: string
  definition: string
  assignedAtUtc: string
}

export interface GroupMemberDashboard {
  learnerId: string
  currentStreakDays: number
  activeDays: number
  retention: number | null
  retentionSample: number
  perStatus: WordStatusCount[]
  leechCount: number
  lastActiveDate: string | null
}

export interface GroupTotals {
  memberCount: number
  medianRetention: number | null
  membersActiveThisWeek: number
}

/** From `GET /api/progress/dashboard/groups/{groupId}`. Names are not included: join `learnerId` with the group detail. */
export interface GroupDashboard {
  groupId: string
  days: number
  from: string
  to: string
  totals: GroupTotals
  members: GroupMemberDashboard[]
}
