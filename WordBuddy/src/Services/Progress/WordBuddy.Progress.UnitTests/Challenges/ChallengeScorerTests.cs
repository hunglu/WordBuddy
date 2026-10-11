using FluentAssertions;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Domain.Challenges;
using static WordBuddy.Progress.UnitTests.Challenges.ChallengeTestKit;

namespace WordBuddy.Progress.UnitTests.Challenges;

public class ChallengeScorerTests
{
    private static readonly DateTime Start = Monday;
    private static readonly DateTime End = Monday.AddDays(7);
    private static readonly Guid User = Guid.NewGuid();

    private static ChallengeScoringWindow Window(DateTime? joined = null) => new(Start, End, joined ?? Start);

    private static int Score(IEnumerable<ReviewLog> logs, ChallengeMetric metric = ChallengeMetric.MemoryPoints, string? zone = null, ChallengeOptions? options = null, DateTime? joined = null) =>
        ChallengeScorer.Score(logs, metric, Window(joined), zone, options ?? new ChallengeOptions());

    /// <summary>The first log of a word (ignored as a new word), one day before the window.</summary>
    private static ReviewLog First(Guid sense, ExerciseType type = ExerciseType.Typing) =>
        Log(User, sense, Start.AddDays(-1), type);

    [Fact]
    public void ChallengeScorer_Score_CorrectDueFirstTryWithoutHintIsTenPoints()
    {
        Guid sense = Guid.NewGuid();

        Score([First(sense), Log(User, sense, Start.AddDays(1))]).Should().Be(10);
    }

    [Fact]
    public void ChallengeScorer_Score_HintedFirstTryIsFivePoints()
    {
        Guid sense = Guid.NewGuid();

        Score([First(sense), Log(User, sense, Start.AddDays(1), hint: true)]).Should().Be(5);
    }

    [Fact]
    public void ChallengeScorer_Score_RecallUpgradeAddsFivePointsAndCountsOnce()
    {
        Guid sense = Guid.NewGuid();
        List<ReviewLog> logs =
        [
            First(sense, ExerciseType.PictureChoice),
            Log(User, sense, Start.AddDays(1), ExerciseType.Typing),
            Log(User, sense, Start.AddDays(2), ExerciseType.Typing),
        ];

        Score(logs).Should().Be(10 + 5 + 10);
        Score(logs, ChallengeMetric.RecallUpgrades).Should().Be(1);
    }

    [Fact]
    public void ChallengeScorer_Score_TypingOnlyHistoryIsNotAnUpgrade()
    {
        Guid sense = Guid.NewGuid();

        Score([First(sense, ExerciseType.Typing), Log(User, sense, Start.AddDays(1), ExerciseType.Typing)], ChallengeMetric.RecallUpgrades).Should().Be(0);
    }

    [Fact]
    public void ChallengeScorer_Score_DailyGoalBonusAtTenCorrectDueReviewsInOneDay()
    {
        List<ReviewLog> logs = [];
        for (int i = 0; i < 10; i++)
        {
            Guid sense = Guid.NewGuid();
            logs.Add(First(sense));
            logs.Add(Log(User, sense, Start.AddDays(1).AddHours(i)));
        }

        Score(logs).Should().Be((10 * 10) + 10);
    }

    [Fact]
    public void ChallengeScorer_Score_NineCorrectDueReviewsEarnNoBonus()
    {
        List<ReviewLog> logs = [];
        for (int i = 0; i < 9; i++)
        {
            Guid sense = Guid.NewGuid();
            logs.Add(First(sense));
            logs.Add(Log(User, sense, Start.AddDays(1).AddHours(i)));
        }

        Score(logs).Should().Be(9 * 10);
    }

    [Fact]
    public void ChallengeScorer_Score_DailyCapLimitsOneDayAt200()
    {
        List<ReviewLog> logs = [];
        for (int i = 0; i < 25; i++)
        {
            Guid sense = Guid.NewGuid();
            logs.Add(First(sense));
            logs.Add(Log(User, sense, Start.AddDays(1).AddMinutes(i)));
        }

        Score(logs).Should().Be(200);
    }

    // 16:30Z and 17:30Z: Ho Chi Minh (UTC+7) is 23:30 and 00:30 (two days); New York (UTC-4) is 12:30 and 13:30 (one day).
    private static List<ReviewLog> TwoAnswersAcrossLocalMidnight()
    {
        Guid a = Guid.NewGuid();
        Guid b = Guid.NewGuid();
        DateTime t1 = new(2026, 10, 6, 16, 30, 0, DateTimeKind.Utc);
        DateTime t2 = new(2026, 10, 6, 17, 30, 0, DateTimeKind.Utc);
        return [First(a), First(b), Log(User, a, t1), Log(User, b, t2)];
    }

    [Fact]
    public void ChallengeScorer_Score_CapIsAppliedPerLocalDay()
    {
        ChallengeOptions options = new() { DailyPointCap = 15 };
        List<ReviewLog> logs = TwoAnswersAcrossLocalMidnight();

        Score(logs, zone: "Asia/Ho_Chi_Minh", options: options).Should().Be(20);
        Score(logs, zone: "America/New_York", options: options).Should().Be(15);
    }

    [Fact]
    public void ChallengeScorer_Score_DailyGoalIsAppliedPerLocalDay()
    {
        ChallengeOptions options = new() { DailyGoalCorrectDue = 2, DailyGoalBonus = 10 };
        List<ReviewLog> logs = TwoAnswersAcrossLocalMidnight();

        Score(logs, zone: "Asia/Ho_Chi_Minh", options: options).Should().Be(20);
        Score(logs, zone: "America/New_York", options: options).Should().Be(30);
    }

    [Fact]
    public void ChallengeScorer_Score_DueReviewsCorrectCountsLocalDaysWithAtLeastOne()
    {
        List<ReviewLog> logs = TwoAnswersAcrossLocalMidnight();

        Score(logs, ChallengeMetric.DueReviewsCorrect, "Asia/Ho_Chi_Minh").Should().Be(2);
        Score(logs, ChallengeMetric.DueReviewsCorrect, "America/New_York").Should().Be(1);
        Score(logs, ChallengeMetric.DueReviewsCorrect, zone: null).Should().Be(1);
    }

    [Fact]
    public void ChallengeScorer_Score_ListeningCorrectCountsOnlyListeningRows()
    {
        Guid a = Guid.NewGuid();
        Guid b = Guid.NewGuid();
        List<ReviewLog> logs =
        [
            First(a), First(b),
            Log(User, a, Start.AddDays(1), ExerciseType.ListeningChoice),
            Log(User, b, Start.AddDays(1), ExerciseType.Typing),
        ];

        Score(logs, ChallengeMetric.ListeningCorrect).Should().Be(1);
    }

    [Fact]
    public void ChallengeScorer_Score_SecondAttemptEarnsNoPointsButCountsAsACorrectDueReview()
    {
        Guid sense = Guid.NewGuid();
        List<ReviewLog> logs = [First(sense), Log(User, sense, Start.AddDays(1), attempt: 2)];

        Score(logs).Should().Be(0);
        Score(logs, ChallengeMetric.DueReviewsCorrect).Should().Be(1);
    }

    public static TheoryData<string> IgnoredRows => new() { "NotDue", "Wrong", "TooFast", "TimingAdjusted", "FirstReview", "BeforeJoin", "BeforeWindow", "AfterWindow" };

    [Theory]
    [MemberData(nameof(IgnoredRows))]
    public void ChallengeScorer_Score_IgnoredRowEarnsNothing(string kind)
    {
        Guid sense = Guid.NewGuid();
        DateTime joined = Start.AddDays(1);
        DateTime inside = Start.AddDays(2);
        List<ReviewLog> logs = kind switch
        {
            "NotDue" => [First(sense), Log(User, sense, inside, due: false)],
            "Wrong" => [First(sense), Log(User, sense, inside, correct: false)],
            "TooFast" => [First(sense), Log(User, sense, inside, ms: 799)],
            "TimingAdjusted" => [First(sense), Log(User, sense, inside, adjusted: true)],
            "FirstReview" => [Log(User, sense, inside)],
            "BeforeJoin" => [First(sense), Log(User, sense, joined.AddHours(-1))],
            "BeforeWindow" => [First(sense), Log(User, sense, Start.AddMinutes(-1))],
            _ => [First(sense), Log(User, sense, End)],
        };

        Score(logs, joined: joined).Should().Be(0);
        Score(logs, ChallengeMetric.DueReviewsCorrect, joined: joined).Should().Be(0);
    }

    [Fact]
    public void ChallengeScorer_Score_AnswerExactlyAtTheFloorCounts()
    {
        Guid sense = Guid.NewGuid();

        Score([First(sense), Log(User, sense, Start.AddDays(1), ms: 800)]).Should().Be(10);
    }

    [Fact]
    public void ChallengeScorer_Score_RebuildGivesTheSameScoreInAnyInputOrder()
    {
        List<ReviewLog> logs = [];
        for (int i = 0; i < 12; i++)
        {
            Guid sense = Guid.NewGuid();
            logs.Add(First(sense, i % 2 == 0 ? ExerciseType.PictureChoice : ExerciseType.Typing));
            logs.Add(Log(User, sense, Start.AddDays(1 + (i % 3)).AddMinutes(i), hint: i % 4 == 0));
        }

        int first = Score(logs, zone: "Asia/Ho_Chi_Minh");
        int second = Score(logs, zone: "Asia/Ho_Chi_Minh");
        int shuffled = Score(logs.AsEnumerable().Reverse(), zone: "Asia/Ho_Chi_Minh");

        second.Should().Be(first);
        shuffled.Should().Be(first);
        first.Should().BeGreaterThan(0);
    }
}
