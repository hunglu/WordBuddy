export const lessons = {
  title: 'Lessons',
  all: 'All',
  allLevels: 'All levels',
  type: {
    Vocabulary: 'Vocabulary',
    Grammar: 'Grammar',
    DailyPhrase: 'Daily Phrases',
  },
  level: {
    Beginner: 'Beginner',
    Intermediate: 'Intermediate',
    Advanced: 'Advanced',
  },
  loading: 'Loading lessons…',
  loadFailed: "Couldn't load lessons right now. Please try again in a moment.",
  noMatch: 'No lessons match these filters yet.',
  detail: {
    loading: 'Loading lesson…',
    loadFailed: "Couldn't load this lesson right now.",
    complete: 'Mark as Complete',
    completed: 'Completed! 🎉',
    saving: 'Saving…',
  },
  list: {
    playAudio: 'Play audio',
    watchVideo: 'Watch video',
    playPronunciation: 'Play pronunciation of {{word}}',
    awaiting: 'Waiting for approval',
  },
} as const
