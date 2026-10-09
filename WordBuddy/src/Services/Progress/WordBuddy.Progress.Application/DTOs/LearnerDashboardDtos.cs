using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.Application.DTOs;

// Dates only: no DTO here carries a time of day, for any viewer.

/// <summary>Dashboard of one learner, computed from review logs for a window of local days.</summary>
/// <param name="LearnerId">The learner.</param>
/// <param name="Days">Window length in local days (7, 30 or 90).</param>
/// <param name="From">First local day of the window.</param>
/// <param name="To">Last local day of the window (the learner's "today").</param>
/// <param name="Activity">Streak, active days, heatmap, daily goal.</param>
/// <param name="Words">Words added, per status, reviews per day.</param>
/// <param name="Retention">True retention.</param>
/// <param name="Struggle">Leech words, weakest skill, slowest words.</param>
/// <param name="Gaming">Quick-wrong, hint and unfinished-session signals.</param>
public sealed record LearnerDashboardDto(
    Guid LearnerId,
    int Days,
    DateOnly From,
    DateOnly To,
    ActivityDto Activity,
    WordsDto Words,
    RetentionDto Retention,
    StruggleDto Struggle,
    GamingSignalsDto Gaming);

/// <summary>Practice activity.</summary>
/// <param name="CurrentStreakDays">Consecutive local days with a review, ending today or yesterday (counted over the last 90 days).</param>
/// <param name="ActiveDaysPerWeek">Distinct review days per ISO week.</param>
/// <param name="Heatmap">Review count for every local day of the window.</param>
/// <param name="DailyGoals">Per day with an issued session: answered words against the first session's size.</param>
public sealed record ActivityDto(
    int CurrentStreakDays,
    IReadOnlyList<WeekActivityDto> ActiveDaysPerWeek,
    IReadOnlyList<DayActivityDto> Heatmap,
    IReadOnlyList<DailyGoalDto> DailyGoals);

/// <summary>Active days in one ISO week.</summary>
/// <param name="WeekStart">Monday of the week.</param>
/// <param name="ActiveDays">Days with at least one review.</param>
public sealed record WeekActivityDto(DateOnly WeekStart, int ActiveDays);

/// <summary>Reviews on one local day.</summary>
/// <param name="Date">Local day.</param>
/// <param name="Reviews">Number of answers.</param>
public sealed record DayActivityDto(DateOnly Date, int Reviews);

/// <summary>Progress toward the day's goal (size of the day's first session).</summary>
/// <param name="Date">Local day.</param>
/// <param name="PlannedItems">Items in the day's first session.</param>
/// <param name="AnsweredWords">Distinct words answered that day.</param>
/// <param name="Percent">Answered divided by planned, capped at 100.</param>
public sealed record DailyGoalDto(DateOnly Date, int PlannedItems, int AnsweredWords, int Percent);

/// <summary>Word list changes and statuses.</summary>
/// <param name="AddedPerDay">Words added per local day by the learner and by supporters.</param>
/// <param name="AddedPerWeek">Same per ISO week (<see cref="WordsAddedDto.Date"/> = Monday).</param>
/// <param name="PerStatus">Active words per status (all statuses listed).</param>
/// <param name="ReviewsPerDay">Review count for every local day of the window.</param>
public sealed record WordsDto(
    IReadOnlyList<WordsAddedDto> AddedPerDay,
    IReadOnlyList<WordsAddedDto> AddedPerWeek,
    IReadOnlyList<WordStatusCountDto> PerStatus,
    IReadOnlyList<DayActivityDto> ReviewsPerDay);

/// <summary>Words added in one bucket (day or week).</summary>
/// <param name="Date">Local day, or Monday of the week.</param>
/// <param name="Self">Added by the learner.</param>
/// <param name="Supporters">Added by supporters, one entry per supporter.</param>
public sealed record WordsAddedDto(DateOnly Date, int Self, IReadOnlyList<SupporterWordsAddedDto> Supporters);

/// <summary>Words one supporter added.</summary>
/// <param name="SupporterId">The supporter's user id.</param>
/// <param name="Count">Words added.</param>
public sealed record SupporterWordsAddedDto(Guid SupporterId, int Count);

/// <summary>Number of active words in one status.</summary>
/// <param name="Status">Word status.</param>
/// <param name="Count">Words.</param>
public sealed record WordStatusCountDto(WordStatus Status, int Count);

/// <summary>True retention: first attempt of due or new words only.</summary>
/// <param name="Overall">Percent correct (0–100), or <see langword="null"/> below the minimum sample.</param>
/// <param name="Sample">Counted answers.</param>
/// <param name="BySkill">Per skill, only skills with answers.</param>
public sealed record RetentionDto(double? Overall, int Sample, IReadOnlyList<SkillRetentionDto> BySkill);

/// <summary>Retention of one skill.</summary>
/// <param name="Skill">Skill.</param>
/// <param name="Retention">Percent correct, or <see langword="null"/> below the minimum sample.</param>
/// <param name="Sample">Counted answers.</param>
public sealed record SkillRetentionDto(VocabularySkill Skill, double? Retention, int Sample);

/// <summary>Where the learner struggles. Word text comes from Content.</summary>
/// <param name="Leeches">Words with status Leech, most lapses first.</param>
/// <param name="WeakestSkill">Skill with the lowest retention (enough answers), or <see langword="null"/>.</param>
/// <param name="SlowestWords">Up to 10 words with the highest median response time (at least 3 answers).</param>
public sealed record StruggleDto(
    IReadOnlyList<LeechWordDto> Leeches,
    VocabularySkill? WeakestSkill,
    IReadOnlyList<SlowWordDto> SlowestWords);

/// <summary>A leech word.</summary>
/// <param name="SenseId">Content's sense id.</param>
/// <param name="Lapses">Times forgotten.</param>
public sealed record LeechWordDto(Guid SenseId, int Lapses);

/// <summary>A slow word.</summary>
/// <param name="SenseId">Content's sense id.</param>
/// <param name="MedianResponseMs">Median response time in milliseconds.</param>
/// <param name="Answers">Answers behind the median.</param>
public sealed record SlowWordDto(Guid SenseId, int MedianResponseMs, int Answers);

/// <summary>Answer-pattern signals. Neutral facts, not a verdict.</summary>
/// <param name="TotalAnswers">Answers in the window.</param>
/// <param name="QuickWrongCount">Wrong answers faster than the threshold.</param>
/// <param name="QuickWrongPercent">Share of all answers (0–100).</param>
/// <param name="HintPercent">Share of answers that used a hint (0–100).</param>
/// <param name="HintRateFlagged">Hint rate is above the configured limit.</param>
/// <param name="UnfinishedSessions">Past sessions with fewer answered words than planned.</param>
public sealed record GamingSignalsDto(
    int TotalAnswers,
    int QuickWrongCount,
    double QuickWrongPercent,
    double HintPercent,
    bool HintRateFlagged,
    int UnfinishedSessions);
