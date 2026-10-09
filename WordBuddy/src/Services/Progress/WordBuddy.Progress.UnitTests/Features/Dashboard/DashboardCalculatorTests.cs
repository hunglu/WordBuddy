using FluentAssertions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Features.Dashboard;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.UnitTests.Features.Dashboard;

/// <summary>Pure calculator: no DB, no clock. Local days follow the client offset (+07:00 in most tests).</summary>
public class DashboardCalculatorTests
{
    // 2026-10-09 03:00 at +07:00 (= 2026-10-08 20:00 UTC). Local today = 2026-10-09.
    private static readonly DateTime Now = new(2026, 10, 8, 20, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Plus7 = TimeSpan.FromHours(7);
    private static readonly DateOnly Today = new(2026, 10, 9);

    private readonly Guid _learnerId = Guid.NewGuid();
    private readonly DashboardOptions _options = new() { MinSample = 10, FastWrongMs = 1500, HintRateFlag = 0.3 };

    private DashboardCalculator Calculator => new(_options);

    /// <summary>UTC time of a local date at a local hour for the +07:00 offset.</summary>
    private static DateTime LocalToUtc(DateOnly date, int hour = 12) =>
        DateTime.SpecifyKind(date.ToDateTime(new TimeOnly(hour, 0)) - Plus7, DateTimeKind.Utc);

    private static DashboardReviewRow Review(
        DateOnly localDate,
        Guid? senseId = null,
        Guid? sessionId = null,
        VocabularySkill skill = VocabularySkill.Meaning,
        bool isCorrect = true,
        int responseMs = 3000,
        bool hintUsed = false,
        bool isDue = true,
        int attemptNo = 1,
        int hour = 12) =>
        new(senseId ?? Guid.NewGuid(), sessionId ?? Guid.NewGuid(), LocalToUtc(localDate, hour), skill, isCorrect, responseMs, hintUsed, isDue, attemptNo);

    private LearnerDashboardDto Calculate(
        IReadOnlyList<DashboardReviewRow>? reviews = null,
        IReadOnlyList<DashboardWordRow>? words = null,
        IReadOnlyList<DashboardMembershipRow>? memberships = null,
        IReadOnlyList<DashboardSessionRow>? sessions = null,
        int days = 30,
        TimeSpan? offset = null) =>
        Calculator.Calculate(_learnerId, days, Now, offset ?? Plus7, reviews ?? [], words ?? [], memberships ?? [], sessions ?? []);

    // Activity

    [Fact]
    public void DashboardCalculator_Calculate_StreakEndingTodayCountsConsecutiveDays()
    {
        DashboardReviewRow[] reviews = [Review(Today), Review(Today.AddDays(-1)), Review(Today.AddDays(-2))];

        Calculate(reviews).Activity.CurrentStreakDays.Should().Be(3);
    }

    [Fact]
    public void DashboardCalculator_Calculate_StreakEndingYesterdayStillCounts()
    {
        DashboardReviewRow[] reviews = [Review(Today.AddDays(-1)), Review(Today.AddDays(-2))];

        Calculate(reviews).Activity.CurrentStreakDays.Should().Be(2);
    }

    [Fact]
    public void DashboardCalculator_Calculate_BrokenStreakIsZero()
    {
        DashboardReviewRow[] reviews = [Review(Today.AddDays(-2)), Review(Today.AddDays(-3))];

        Calculate(reviews).Activity.CurrentStreakDays.Should().Be(0);
    }

    [Fact]
    public void DashboardCalculator_Calculate_StreakStopsAtGap()
    {
        DashboardReviewRow[] reviews = [Review(Today), Review(Today.AddDays(-2))];

        Calculate(reviews).Activity.CurrentStreakDays.Should().Be(1);
    }

    [Fact]
    public void DashboardCalculator_Calculate_StreakLooksBeyondShortWindow()
    {
        DashboardReviewRow[] reviews = Enumerable.Range(0, 20).Select(i => Review(Today.AddDays(-i))).ToArray();

        Calculate(reviews, days: 7).Activity.CurrentStreakDays.Should().Be(20);
    }

    [Fact]
    public void DashboardCalculator_Calculate_ActiveDaysPerWeekCountsDistinctDays()
    {
        // 2026-10-09 is a Friday; its ISO week starts Monday 2026-10-05.
        DashboardReviewRow[] reviews =
        [
            Review(Today), Review(Today), Review(Today.AddDays(-1)),
            Review(new DateOnly(2026, 10, 2)),
        ];

        List<WeekActivityDto> weeks = Calculate(reviews, days: 30).Activity.ActiveDaysPerWeek.ToList();

        weeks.Single(w => w.WeekStart == new DateOnly(2026, 10, 5)).ActiveDays.Should().Be(2);
        weeks.Single(w => w.WeekStart == new DateOnly(2026, 9, 28)).ActiveDays.Should().Be(1);
        weeks.Should().OnlyContain(w => w.WeekStart.DayOfWeek == DayOfWeek.Monday);
    }

    [Fact]
    public void DashboardCalculator_Calculate_HeatmapUsesLocalDateAcrossNonUtcOffset()
    {
        // 2026-10-08 20:00 UTC is 2026-10-09 at +07:00 — the local day, not the UTC day.
        DashboardReviewRow late = new(Guid.NewGuid(), Guid.NewGuid(), Now, VocabularySkill.Meaning, true, 3000, false, true, 1);

        LearnerDashboardDto dashboard = Calculate([late]);

        dashboard.Activity.Heatmap.Single(d => d.Date == Today).Reviews.Should().Be(1);
        dashboard.Activity.Heatmap.Single(d => d.Date == Today.AddDays(-1)).Reviews.Should().Be(0);
    }

    [Fact]
    public void DashboardCalculator_Calculate_HeatmapCoversEveryDayOfWindow()
    {
        LearnerDashboardDto dashboard = Calculate(days: 7);

        dashboard.From.Should().Be(Today.AddDays(-6));
        dashboard.To.Should().Be(Today);
        dashboard.Activity.Heatmap.Should().HaveCount(7);
        dashboard.Activity.Heatmap.Select(d => d.Date).Should().BeInAscendingOrder();
    }

    [Fact]
    public void DashboardCalculator_Calculate_ReviewsOutsideWindowAreIgnoredExceptForStreak()
    {
        DashboardReviewRow[] reviews = [Review(Today.AddDays(-10))];

        LearnerDashboardDto dashboard = Calculate(reviews, days: 7);

        dashboard.Gaming.TotalAnswers.Should().Be(0);
        dashboard.Activity.Heatmap.Sum(d => d.Reviews).Should().Be(0);
    }

    [Fact]
    public void DashboardCalculator_Calculate_DailyGoalUsesFirstSessionAndDistinctWords()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        Guid wordA = Guid.NewGuid();
        Guid wordB = Guid.NewGuid();
        DashboardSessionRow[] sessions =
        [
            new(second, LocalToUtc(Today, 18), 99),
            new(first, LocalToUtc(Today, 8), 4),
        ];
        DashboardReviewRow[] reviews =
        [
            Review(Today, wordA, first), Review(Today, wordA, first, attemptNo: 2), Review(Today, wordB, first),
        ];

        DailyGoalDto goal = Calculate(reviews, sessions: sessions).Activity.DailyGoals.Single();

        goal.PlannedItems.Should().Be(4);
        goal.AnsweredWords.Should().Be(2);
        goal.Percent.Should().Be(50);
    }

    [Fact]
    public void DashboardCalculator_Calculate_DailyGoalIsCappedAt100()
    {
        Guid session = Guid.NewGuid();
        DashboardSessionRow[] sessions = [new(session, LocalToUtc(Today, 8), 1)];
        DashboardReviewRow[] reviews = [Review(Today, sessionId: session), Review(Today, sessionId: session)];

        Calculate(reviews, sessions: sessions).Activity.DailyGoals.Single().Percent.Should().Be(100);
    }

    // Words

    [Fact]
    public void DashboardCalculator_Calculate_WordsAddedSplitSelfAndSupporter()
    {
        Guid supporterA = Guid.NewGuid();
        Guid supporterB = Guid.NewGuid();
        DashboardMembershipRow[] memberships =
        [
            new(Guid.NewGuid(), _learnerId, LocalToUtc(Today)),
            new(Guid.NewGuid(), _learnerId, LocalToUtc(Today)),
            new(Guid.NewGuid(), supporterA, LocalToUtc(Today)),
            new(Guid.NewGuid(), supporterB, LocalToUtc(Today)),
            new(Guid.NewGuid(), supporterB, LocalToUtc(Today)),
            new(Guid.NewGuid(), _learnerId, LocalToUtc(Today.AddDays(-1))),
        ];

        WordsDto words = Calculate(memberships: memberships).Words;

        WordsAddedDto day = words.AddedPerDay.Single(d => d.Date == Today);
        day.Self.Should().Be(2);
        day.Supporters.Should().BeEquivalentTo(
            new[] { new SupporterWordsAddedDto(supporterA, 1), new SupporterWordsAddedDto(supporterB, 2) });
        words.AddedPerDay.Single(d => d.Date == Today.AddDays(-1)).Self.Should().Be(1);
        words.AddedPerWeek.Single(w => w.Date == new DateOnly(2026, 10, 5)).Self.Should().Be(3);
    }

    [Fact]
    public void DashboardCalculator_Calculate_WordsPerStatusListsEveryStatus()
    {
        DashboardWordRow[] states =
        [
            new(Guid.NewGuid(), WordStatus.New, 0), new(Guid.NewGuid(), WordStatus.New, 0),
            new(Guid.NewGuid(), WordStatus.Mastered, 0), new(Guid.NewGuid(), WordStatus.Leech, 5),
        ];

        List<WordStatusCountDto> perStatus = Calculate(words: states).Words.PerStatus.ToList();

        perStatus.Should().HaveCount(Enum.GetValues<WordStatus>().Length);
        perStatus.Single(s => s.Status == WordStatus.New).Count.Should().Be(2);
        perStatus.Single(s => s.Status == WordStatus.Mastered).Count.Should().Be(1);
        perStatus.Single(s => s.Status == WordStatus.Learning).Count.Should().Be(0);
    }

    [Fact]
    public void DashboardCalculator_Calculate_ReviewsPerDayCountsRows()
    {
        DashboardReviewRow[] reviews = [Review(Today), Review(Today), Review(Today.AddDays(-3))];

        WordsDto words = Calculate(reviews).Words;

        words.ReviewsPerDay.Single(d => d.Date == Today).Reviews.Should().Be(2);
        words.ReviewsPerDay.Single(d => d.Date == Today.AddDays(-3)).Reviews.Should().Be(1);
    }

    // Retention

    [Fact]
    public void DashboardCalculator_Calculate_RetentionCountsOnlyFirstAttemptsOfDueReviews()
    {
        List<DashboardReviewRow> reviews = [];
        reviews.AddRange(Enumerable.Range(0, 8).Select(_ => Review(Today)));                       // 8 correct
        reviews.AddRange(Enumerable.Range(0, 2).Select(_ => Review(Today, isCorrect: false)));     // 2 wrong
        reviews.AddRange(Enumerable.Range(0, 5).Select(_ => Review(Today, isCorrect: false, attemptNo: 2))); // retries: ignored
        reviews.AddRange(Enumerable.Range(0, 5).Select(_ => Review(Today, isCorrect: false, isDue: false))); // not due: ignored

        RetentionDto retention = Calculate(reviews).Retention;

        retention.Sample.Should().Be(10);
        retention.Overall.Should().Be(80);
    }

    [Fact]
    public void DashboardCalculator_Calculate_RetentionPerSkill()
    {
        List<DashboardReviewRow> reviews = [];
        reviews.AddRange(Enumerable.Range(0, 10).Select(_ => Review(Today, skill: VocabularySkill.Meaning)));
        reviews.AddRange(Enumerable.Range(0, 10).Select(i => Review(Today, skill: VocabularySkill.Spelling, isCorrect: i < 5)));

        RetentionDto retention = Calculate(reviews).Retention;

        retention.BySkill.Single(s => s.Skill == VocabularySkill.Meaning).Retention.Should().Be(100);
        retention.BySkill.Single(s => s.Skill == VocabularySkill.Spelling).Retention.Should().Be(50);
        retention.BySkill.Should().NotContain(s => s.Skill == VocabularySkill.Listening);
    }

    [Fact]
    public void DashboardCalculator_Calculate_RetentionIsNullBelowMinSample()
    {
        DashboardReviewRow[] reviews = Enumerable.Range(0, 9).Select(_ => Review(Today)).ToArray();

        RetentionDto retention = Calculate(reviews).Retention;

        retention.Overall.Should().BeNull();
        retention.Sample.Should().Be(9);
        retention.BySkill.Single().Retention.Should().BeNull();
    }

    // Struggle

    [Fact]
    public void DashboardCalculator_Calculate_LeechesSortedByLapses()
    {
        Guid few = Guid.NewGuid();
        Guid many = Guid.NewGuid();
        DashboardWordRow[] states = [new(few, WordStatus.Leech, 4), new(many, WordStatus.Leech, 9), new(Guid.NewGuid(), WordStatus.Review, 2)];

        List<LeechWordDto> leeches = Calculate(words: states).Struggle.Leeches.ToList();

        leeches.Select(l => l.SenseId).Should().Equal(many, few);
    }

    [Fact]
    public void DashboardCalculator_Calculate_WeakestSkillIsLowestWithEnoughAnswers()
    {
        List<DashboardReviewRow> reviews = [];
        reviews.AddRange(Enumerable.Range(0, 10).Select(_ => Review(Today, skill: VocabularySkill.Meaning)));
        reviews.AddRange(Enumerable.Range(0, 10).Select(i => Review(Today, skill: VocabularySkill.Spelling, isCorrect: i < 4)));
        // Pronunciation is worst (0 %) but only 3 answers — below MinSample, so not eligible.
        reviews.AddRange(Enumerable.Range(0, 3).Select(_ => Review(Today, skill: VocabularySkill.Pronunciation, isCorrect: false)));

        Calculate(reviews).Struggle.WeakestSkill.Should().Be(VocabularySkill.Spelling);
    }

    [Fact]
    public void DashboardCalculator_Calculate_WeakestSkillIsNullWithoutEnoughData()
    {
        Calculate([Review(Today)]).Struggle.WeakestSkill.Should().BeNull();
    }

    [Fact]
    public void DashboardCalculator_Calculate_SlowestWordsUseMedianAndNeedThreeAnswers()
    {
        Guid slow = Guid.NewGuid();
        Guid fast = Guid.NewGuid();
        Guid twoAnswers = Guid.NewGuid();
        DashboardReviewRow[] reviews =
        [
            Review(Today, slow, responseMs: 9000), Review(Today, slow, responseMs: 8000), Review(Today, slow, responseMs: 1000),
            Review(Today, fast, responseMs: 1000), Review(Today, fast, responseMs: 1200), Review(Today, fast, responseMs: 5000),
            Review(Today, twoAnswers, responseMs: 20000), Review(Today, twoAnswers, responseMs: 20000),
        ];

        List<SlowWordDto> slowest = Calculate(reviews).Struggle.SlowestWords.ToList();

        slowest.Select(w => w.SenseId).Should().Equal(slow, fast);
        slowest[0].MedianResponseMs.Should().Be(8000);
        slowest[0].Answers.Should().Be(3);
        slowest[1].MedianResponseMs.Should().Be(1200);
    }

    [Fact]
    public void DashboardCalculator_Calculate_SlowestWordsAreCappedAtTen()
    {
        DashboardReviewRow[] reviews = Enumerable.Range(0, 12)
            .SelectMany(_ =>
            {
                Guid sense = Guid.NewGuid();
                return Enumerable.Range(0, 3).Select(__ => Review(Today, sense));
            })
            .ToArray();

        Calculate(reviews).Struggle.SlowestWords.Should().HaveCount(10);
    }

    // Gaming

    [Fact]
    public void DashboardCalculator_Calculate_QuickWrongCountsOnlyWrongAnswersBelowThreshold()
    {
        DashboardReviewRow[] reviews =
        [
            Review(Today, isCorrect: false, responseMs: 500),
            Review(Today, isCorrect: false, responseMs: 1499),
            Review(Today, isCorrect: false, responseMs: 1500), // not below the threshold
            Review(Today, isCorrect: true, responseMs: 300),   // fast but right
        ];

        GamingSignalsDto gaming = Calculate(reviews).Gaming;

        gaming.TotalAnswers.Should().Be(4);
        gaming.QuickWrongCount.Should().Be(2);
        gaming.QuickWrongPercent.Should().Be(50);
    }

    [Theory]
    [InlineData(2, 10, false)]
    [InlineData(3, 10, false)] // exactly 0.3 is not above the limit
    [InlineData(4, 10, true)]
    public void DashboardCalculator_Calculate_HintRateFlaggedOnlyAboveLimit(int hints, int total, bool expectedFlag)
    {
        DashboardReviewRow[] reviews = Enumerable.Range(0, total).Select(i => Review(Today, hintUsed: i < hints)).ToArray();

        Calculate(reviews).Gaming.HintRateFlagged.Should().Be(expectedFlag);
    }

    [Fact]
    public void DashboardCalculator_Calculate_HintPercentIsShareOfAnswers()
    {
        DashboardReviewRow[] reviews = [Review(Today, hintUsed: true), Review(Today), Review(Today), Review(Today)];

        Calculate(reviews).Gaming.HintPercent.Should().Be(25);
    }

    [Fact]
    public void DashboardCalculator_Calculate_UnfinishedSessionsExcludeTodaysOpenSession()
    {
        Guid unfinishedYesterday = Guid.NewGuid();
        Guid finishedYesterday = Guid.NewGuid();
        Guid openToday = Guid.NewGuid();
        Guid wordA = Guid.NewGuid();
        Guid wordB = Guid.NewGuid();
        DashboardSessionRow[] sessions =
        [
            new(unfinishedYesterday, LocalToUtc(Today.AddDays(-1)), 3),
            new(finishedYesterday, LocalToUtc(Today.AddDays(-1), 18), 2),
            new(openToday, LocalToUtc(Today, 8), 5),
        ];
        DashboardReviewRow[] reviews =
        [
            Review(Today.AddDays(-1), wordA, unfinishedYesterday),
            Review(Today.AddDays(-1), wordA, finishedYesterday), Review(Today.AddDays(-1), wordB, finishedYesterday),
        ];

        Calculate(reviews, sessions: sessions).Gaming.UnfinishedSessions.Should().Be(1);
    }

    [Fact]
    public void DashboardCalculator_Calculate_SessionWithNoAnswersCountsAsUnfinished()
    {
        DashboardSessionRow[] sessions = [new(Guid.NewGuid(), LocalToUtc(Today.AddDays(-2)), 4)];

        Calculate(sessions: sessions).Gaming.UnfinishedSessions.Should().Be(1);
    }

    // Rebuild and privacy

    [Fact]
    public void DashboardCalculator_Calculate_SameRowsGiveIdenticalOutput()
    {
        Guid session = Guid.NewGuid();
        DashboardReviewRow[] reviews = Enumerable.Range(0, 25)
            .Select(i => Review(Today.AddDays(-(i % 5)), sessionId: session, isCorrect: i % 3 != 0, responseMs: 500 + (i * 200), hintUsed: i % 4 == 0))
            .ToArray();
        DashboardWordRow[] states = [new(Guid.NewGuid(), WordStatus.Leech, 6), new(Guid.NewGuid(), WordStatus.New, 0)];
        DashboardMembershipRow[] memberships = [new(Guid.NewGuid(), _learnerId, LocalToUtc(Today)), new(Guid.NewGuid(), Guid.NewGuid(), LocalToUtc(Today))];
        DashboardSessionRow[] sessions = [new(session, LocalToUtc(Today.AddDays(-1)), 30)];

        LearnerDashboardDto first = Calculate(reviews, states, memberships, sessions);
        LearnerDashboardDto second = Calculate(reviews, states, memberships, sessions);

        second.Should().BeEquivalentTo(first);
    }

    [Fact]
    public void LearnerDashboardDto_Shape_HasNoTimeOfDayField()
    {
        Type[] dashboardDtos =
        [
            typeof(LearnerDashboardDto), typeof(ActivityDto), typeof(WeekActivityDto), typeof(DayActivityDto),
            typeof(DailyGoalDto), typeof(WordsDto), typeof(WordsAddedDto), typeof(SupporterWordsAddedDto),
            typeof(WordStatusCountDto), typeof(RetentionDto), typeof(SkillRetentionDto), typeof(StruggleDto),
            typeof(LeechWordDto), typeof(SlowWordDto), typeof(GamingSignalsDto),
        ];
        Type[] timeTypes = [typeof(DateTime), typeof(DateTime?), typeof(DateTimeOffset), typeof(TimeOnly), typeof(TimeSpan)];

        IEnumerable<string> offenders = dashboardDtos
            .SelectMany(t => t.GetProperties().Select(p => (Type: t, Property: p)))
            .Where(x => timeTypes.Contains(x.Property.PropertyType))
            .Select(x => $"{x.Type.Name}.{x.Property.Name}");

        offenders.Should().BeEmpty();
    }
}
