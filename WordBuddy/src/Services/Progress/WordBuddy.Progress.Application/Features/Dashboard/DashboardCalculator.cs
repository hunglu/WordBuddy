using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Features.VocabularySrs;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.Application.Features.Dashboard;

/// <summary>
/// Computes every dashboard number from source rows. Pure: no I/O, no clock, no stored aggregates —
/// the same rows always give the same result, so a "rebuild" is just a cache delete. All "days" are
/// the learner's local days (server UTC + the client offset). Output carries dates only, never a time of day.
/// </summary>
public sealed class DashboardCalculator
{
    /// <summary>Largest window a caller can ask for. Rows older than this are never needed.</summary>
    public const int MaxDays = 90;

    private const int SlowWordsTake = 10;
    private const int LeechesTake = 10;
    private const int SlowWordMinAnswers = 3;

    private readonly DashboardOptions _options;

    public DashboardCalculator(DashboardOptions options)
    {
        _options = options;
    }

    /// <summary>Returns the UTC range [from, to) that covers the last <see cref="MaxDays"/> local days.</summary>
    public static (DateTime FromUtc, DateTime ToUtc) SourceRange(DateTime nowUtc, TimeSpan offset)
    {
        (DateTime todayStartUtc, DateTime todayEndUtc) = ClientDateTime.TodayUtcRange(nowUtc, offset);
        return (todayStartUtc.AddDays(-(MaxDays - 1)), todayEndUtc);
    }

    /// <summary>Builds the dashboard. <paramref name="reviews"/> may cover more than the window
    /// (the streak looks back up to <see cref="MaxDays"/> days); other metrics use the window only.</summary>
    public LearnerDashboardDto Calculate(
        Guid learnerId,
        int days,
        DateTime nowUtc,
        TimeSpan offset,
        IReadOnlyList<DashboardReviewRow> reviews,
        IReadOnlyList<DashboardWordRow> words,
        IReadOnlyList<DashboardMembershipRow> memberships,
        IReadOnlyList<DashboardSessionRow> sessions)
    {
        DateOnly today = LocalDate(nowUtc, offset);
        DateOnly from = today.AddDays(-(days - 1));

        List<DashboardReviewRow> windowReviews = reviews
            .Where(r => InWindow(LocalDate(r.OccurredAtUtc, offset), from, today))
            .ToList();
        List<DashboardSessionRow> windowSessions = sessions
            .Where(s => InWindow(LocalDate(s.IssuedAtUtc, offset), from, today))
            .ToList();

        List<DayActivityDto> perDay = BuildReviewsPerDay(windowReviews, from, today, offset);

        ActivityDto activity = new(
            CurrentStreak(reviews, today, offset),
            BuildActiveDaysPerWeek(windowReviews, from, today, offset),
            perDay,
            BuildDailyGoals(windowReviews, windowSessions, offset));

        WordsDto wordsDto = new(
            BuildWordsAdded(memberships, learnerId, from, today, offset, byWeek: false),
            BuildWordsAdded(memberships, learnerId, from, today, offset, byWeek: true),
            BuildStatusCounts(words),
            perDay);

        RetentionDto retention = BuildRetention(windowReviews);

        StruggleDto struggle = new(
            words.Where(w => w.Status == WordStatus.Leech)
                .OrderByDescending(w => w.Lapses)
                .ThenBy(w => w.SenseId)
                .Take(LeechesTake)
                .Select(w => new LeechWordDto(w.SenseId, w.Lapses))
                .ToList(),
            WeakestSkill(retention),
            BuildSlowestWords(windowReviews));

        GamingSignalsDto gaming = BuildGaming(windowReviews, windowSessions, nowUtc);

        return new LearnerDashboardDto(learnerId, days, from, today, activity, wordsDto, retention, struggle, gaming);
    }

    /// <summary>
    /// Builds the group dashboard: one row per member from <see cref="Calculate"/>, plus group totals.
    /// Pure, like <see cref="Calculate"/>. A member with no rows gets a row of zeros, never an error.
    /// Dates only, never a time of day.
    /// </summary>
    public GroupDashboardDto CalculateGroup(
        Guid groupId,
        int days,
        DateTime nowUtc,
        TimeSpan offset,
        IReadOnlyList<Guid> memberIds,
        IReadOnlyDictionary<Guid, IReadOnlyList<DashboardReviewRow>> reviews,
        IReadOnlyDictionary<Guid, IReadOnlyList<DashboardWordRow>> words,
        IReadOnlyDictionary<Guid, IReadOnlyList<DashboardMembershipRow>> memberships,
        IReadOnlyDictionary<Guid, IReadOnlyList<DashboardSessionRow>> sessions)
    {
        DateOnly today = LocalDate(nowUtc, offset);
        DateOnly from = today.AddDays(-(days - 1));
        DateOnly weekStart = today.AddDays(-6);

        List<GroupMemberDashboardDto> members = [];
        foreach (Guid memberId in memberIds.Distinct().OrderBy(id => id))
        {
            IReadOnlyList<DashboardReviewRow> memberReviews = reviews.GetValueOrDefault(memberId) ?? [];
            LearnerDashboardDto dashboard = Calculate(
                memberId,
                days,
                nowUtc,
                offset,
                memberReviews,
                words.GetValueOrDefault(memberId) ?? [],
                memberships.GetValueOrDefault(memberId) ?? [],
                sessions.GetValueOrDefault(memberId) ?? []);

            DateOnly? lastActive = memberReviews.Count > 0
                ? memberReviews.Max(r => LocalDate(r.OccurredAtUtc, offset))
                : null;

            members.Add(new GroupMemberDashboardDto(
                memberId,
                dashboard.Activity.CurrentStreakDays,
                dashboard.Activity.Heatmap.Count(d => d.Reviews > 0),
                dashboard.Retention.Overall,
                dashboard.Retention.Sample,
                dashboard.Words.PerStatus,
                dashboard.Words.PerStatus.Where(s => s.Status == WordStatus.Leech).Sum(s => s.Count),
                lastActive));
        }

        List<double> retentions = members.Where(m => m.Retention.HasValue).Select(m => m.Retention!.Value).Order().ToList();
        double? median = retentions.Count == 0
            ? null
            : Math.Round(retentions.Count % 2 == 1
                ? retentions[retentions.Count / 2]
                : (retentions[(retentions.Count / 2) - 1] + retentions[retentions.Count / 2]) / 2.0, 1);

        GroupTotalsDto totals = new(
            members.Count,
            median,
            members.Count(m => m.LastActiveDate.HasValue && m.LastActiveDate.Value >= weekStart));

        return new GroupDashboardDto(groupId, days, from, today, totals, members);
    }

    private static DateOnly LocalDate(DateTime utc, TimeSpan offset) => DateOnly.FromDateTime(utc + offset);

    private static bool InWindow(DateOnly date, DateOnly from, DateOnly to) => date >= from && date <= to;

    private static DateOnly WeekStart(DateOnly date) => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    // Activity

    private static int CurrentStreak(IReadOnlyList<DashboardReviewRow> reviews, DateOnly today, TimeSpan offset)
    {
        HashSet<DateOnly> active = reviews.Select(r => LocalDate(r.OccurredAtUtc, offset)).ToHashSet();

        DateOnly cursor = today;
        if (!active.Contains(cursor))
        {
            cursor = today.AddDays(-1);
        }

        int streak = 0;
        while (active.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return streak;
    }

    private static List<DayActivityDto> BuildReviewsPerDay(
        IReadOnlyList<DashboardReviewRow> windowReviews, DateOnly from, DateOnly today, TimeSpan offset)
    {
        Dictionary<DateOnly, int> counts = windowReviews
            .GroupBy(r => LocalDate(r.OccurredAtUtc, offset))
            .ToDictionary(g => g.Key, g => g.Count());

        List<DayActivityDto> result = [];
        for (DateOnly day = from; day <= today; day = day.AddDays(1))
        {
            result.Add(new DayActivityDto(day, counts.GetValueOrDefault(day)));
        }

        return result;
    }

    private static List<WeekActivityDto> BuildActiveDaysPerWeek(
        IReadOnlyList<DashboardReviewRow> windowReviews, DateOnly from, DateOnly today, TimeSpan offset)
    {
        Dictionary<DateOnly, int> activeDays = windowReviews
            .Select(r => LocalDate(r.OccurredAtUtc, offset))
            .Distinct()
            .GroupBy(WeekStart)
            .ToDictionary(g => g.Key, g => g.Count());

        List<WeekActivityDto> result = [];
        for (DateOnly week = WeekStart(from); week <= today; week = week.AddDays(7))
        {
            result.Add(new WeekActivityDto(week, activeDays.GetValueOrDefault(week)));
        }

        return result;
    }

    /// <summary>
    /// Daily goal rule: per local day with a session, <c>planned</c> = planned items of the day's
    /// first session only; <c>answered</c> = distinct words answered that day across all sessions;
    /// percent = answered / planned, capped at 100.
    /// </summary>
    private static List<DailyGoalDto> BuildDailyGoals(
        IReadOnlyList<DashboardReviewRow> windowReviews, IReadOnlyList<DashboardSessionRow> windowSessions, TimeSpan offset)
    {
        Dictionary<DateOnly, int> answeredPerDay = windowReviews
            .GroupBy(r => LocalDate(r.OccurredAtUtc, offset))
            .ToDictionary(g => g.Key, g => g.Select(r => r.SenseId).Distinct().Count());

        return windowSessions
            .GroupBy(s => LocalDate(s.IssuedAtUtc, offset))
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                int planned = g.OrderBy(s => s.IssuedAtUtc).First().PlannedCount;
                int answered = answeredPerDay.GetValueOrDefault(g.Key);
                int percent = planned > 0 ? Math.Min(100, answered * 100 / planned) : 0;
                return new DailyGoalDto(g.Key, planned, answered, percent);
            })
            .ToList();
    }

    // Words

    private static List<WordsAddedDto> BuildWordsAdded(
        IReadOnlyList<DashboardMembershipRow> memberships,
        Guid learnerId,
        DateOnly from,
        DateOnly today,
        TimeSpan offset,
        bool byWeek)
    {
        return memberships
            .Select(m => (Date: LocalDate(m.AddedAtUtc, offset), m.AddedBy))
            .Where(m => InWindow(m.Date, from, today))
            .GroupBy(m => byWeek ? WeekStart(m.Date) : m.Date)
            .OrderBy(g => g.Key)
            .Select(g => new WordsAddedDto(
                g.Key,
                g.Count(m => m.AddedBy == learnerId),
                g.Where(m => m.AddedBy != learnerId)
                    .GroupBy(m => m.AddedBy)
                    .OrderBy(s => s.Key)
                    .Select(s => new SupporterWordsAddedDto(s.Key, s.Count()))
                    .ToList()))
            .ToList();
    }

    private static List<WordStatusCountDto> BuildStatusCounts(IReadOnlyList<DashboardWordRow> words) =>
        Enum.GetValues<WordStatus>()
            .Select(status => new WordStatusCountDto(status, words.Count(w => w.Status == status)))
            .ToList();

    // Retention

    private RetentionDto BuildRetention(IReadOnlyList<DashboardReviewRow> windowReviews)
    {
        List<DashboardReviewRow> firstAttempts = windowReviews.Where(r => r.IsDue && r.AttemptNo == 1).ToList();

        List<SkillRetentionDto> bySkill = firstAttempts
            .GroupBy(r => r.Skill)
            .OrderBy(g => g.Key)
            .Select(g => new SkillRetentionDto(g.Key, Percent(g.Count(r => r.IsCorrect), g.Count()), g.Count()))
            .ToList();

        return new RetentionDto(
            Percent(firstAttempts.Count(r => r.IsCorrect), firstAttempts.Count),
            firstAttempts.Count,
            bySkill);
    }

    private double? Percent(int correct, int sample) =>
        sample >= _options.MinSample && sample > 0 ? Math.Round(correct * 100.0 / sample, 1) : null;

    private static VocabularySkill? WeakestSkill(RetentionDto retention) =>
        retention.BySkill
            .Where(s => s.Retention.HasValue)
            .OrderBy(s => s.Retention)
            .ThenBy(s => s.Skill)
            .Select(s => (VocabularySkill?)s.Skill)
            .FirstOrDefault();

    // Struggle

    private static List<SlowWordDto> BuildSlowestWords(IReadOnlyList<DashboardReviewRow> windowReviews) =>
        windowReviews
            .GroupBy(r => r.SenseId)
            .Where(g => g.Count() >= SlowWordMinAnswers)
            .Select(g => new SlowWordDto(g.Key, Median(g.Select(r => r.ResponseMs).ToList()), g.Count()))
            .OrderByDescending(w => w.MedianResponseMs)
            .ThenBy(w => w.SenseId)
            .Take(SlowWordsTake)
            .ToList();

    private static int Median(List<int> values)
    {
        values.Sort();
        int middle = values.Count / 2;
        return values.Count % 2 == 1 ? values[middle] : (int)Math.Round((values[middle - 1] + values[middle]) / 2.0);
    }

    // Gaming

    private GamingSignalsDto BuildGaming(
        IReadOnlyList<DashboardReviewRow> windowReviews,
        IReadOnlyList<DashboardSessionRow> windowSessions,
        DateTime nowUtc)
    {
        int total = windowReviews.Count;
        int quickWrong = windowReviews.Count(r => !r.IsCorrect && r.ResponseMs < _options.FastWrongMs);
        int hints = windowReviews.Count(r => r.HintUsed);
        double hintRate = total > 0 ? (double)hints / total : 0;

        Dictionary<Guid, int> answeredPerSession = windowReviews
            .GroupBy(r => r.SessionId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.SenseId).Distinct().Count());

        // Unfinished = expired with fewer distinct words answered than planned. Open sessions are never counted.
        int unfinished = windowSessions.Count(s =>
            s.ExpiresAtUtc <= nowUtc
            && answeredPerSession.GetValueOrDefault(s.SessionId) < s.PlannedCount);

        return new GamingSignalsDto(
            total,
            quickWrong,
            total > 0 ? Math.Round(quickWrong * 100.0 / total, 1) : 0,
            Math.Round(hintRate * 100, 1),
            hintRate > _options.HintRateFlag,
            unfinished);
    }
}
