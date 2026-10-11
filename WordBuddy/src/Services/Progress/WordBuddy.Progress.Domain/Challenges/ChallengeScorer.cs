namespace WordBuddy.Progress.Domain.Challenges;

/// <summary>The window and join time a score is counted in.</summary>
/// <param name="StartsAtUtc">Challenge start.</param>
/// <param name="EndsAtUtc">Challenge end (exclusive).</param>
/// <param name="JoinedAtUtc">When the learner joined. Earlier answers do not count.</param>
public sealed record ChallengeScoringWindow(DateTime StartsAtUtc, DateTime EndsAtUtc, DateTime JoinedAtUtc);

/// <summary>
/// Pure score projection of one learner from review logs. The same logs, window, zone and options
/// always give the same score, so a rebuild is safe at any time.
/// </summary>
/// <remarks>
/// A counted row is: due, correct, not timing-adjusted, not faster than the floor, and not the first
/// log of its word (new word). Choice exercises are "recognise", typing is "recall".
/// MemoryPoints: +10 for a counted first try without hint, +5 with hint, +5 for a recall upgrade
/// (first correct typing after only correct choice answers), +bonus once per local day at the goal;
/// the sum per local day is capped (MemoryPoints only; Listening Sprint counts answers uncapped). Counts per local day use the learner time zone.
/// </remarks>
public static class ChallengeScorer
{
    private const int FirstTryPoints = 10;
    private const int HintedFirstTryPoints = 5;
    private const int RecallUpgradePoints = 5;

    /// <summary>Scores one learner. <paramref name="logs"/> must hold the window logs plus the earlier logs of the same words (history flags).</summary>
    public static int Score(
        IEnumerable<ReviewLog> logs,
        ChallengeMetric metric,
        ChallengeScoringWindow window,
        string? timeZoneId,
        ChallengeOptions options)
    {
        Dictionary<DateOnly, int> points = [];
        Dictionary<DateOnly, int> correctDue = [];
        Dictionary<DateOnly, int> listening = [];
        int recallUpgrades = 0;

        DateTime from = window.JoinedAtUtc > window.StartsAtUtc ? window.JoinedAtUtc : window.StartsAtUtc;
        Dictionary<Guid, SenseHistory> history = [];

        foreach (ReviewLog log in logs.OrderBy(l => l.OccurredAtUtc).ThenBy(l => l.AttemptNo).ThenBy(l => l.Id))
        {
            bool isFirst = !history.TryGetValue(log.SenseId, out SenseHistory? prior);
            prior ??= new SenseHistory();

            bool inWindow = log.OccurredAtUtc >= from && log.OccurredAtUtc < window.EndsAtUtc;
            bool counted = inWindow
                && !isFirst
                && log.IsDue
                && log.IsCorrect
                && !log.TimingAdjusted
                && log.ResponseMs >= options.FastAnswerFloorMs;

            if (counted)
            {
                DateOnly day = LearnerDayResolver.ToLocalDay(log.OccurredAtUtc, timeZoneId);
                correctDue[day] = correctDue.GetValueOrDefault(day) + 1;

                if (log.AttemptNo == 1)
                {
                    points[day] = points.GetValueOrDefault(day) + (log.HintUsed ? HintedFirstTryPoints : FirstTryPoints);
                }

                if (log.ExerciseType == ExerciseType.Typing && prior.CorrectChoice && !prior.CorrectTyping)
                {
                    recallUpgrades++;
                    points[day] = points.GetValueOrDefault(day) + RecallUpgradePoints;
                }

                if (log.ExerciseType == ExerciseType.ListeningChoice)
                {
                    listening[day] = listening.GetValueOrDefault(day) + 1;
                }
            }

            if (log.IsCorrect)
            {
                prior.CorrectTyping |= log.ExerciseType == ExerciseType.Typing;
                prior.CorrectChoice |= log.ExerciseType != ExerciseType.Typing;
            }

            history[log.SenseId] = prior;
        }

        return metric switch
        {
            ChallengeMetric.MemoryPoints => correctDue.Keys.Union(points.Keys)
                .Sum(day => Math.Min(
                    points.GetValueOrDefault(day) + (correctDue.GetValueOrDefault(day) >= options.DailyGoalCorrectDue ? options.DailyGoalBonus : 0),
                    options.DailyPointCap)),
            ChallengeMetric.DueReviewsCorrect => correctDue.Count,
            ChallengeMetric.RecallUpgrades => recallUpgrades,
            ChallengeMetric.ListeningCorrect => listening.Values.Sum(),
            _ => 0,
        };
    }

    private sealed class SenseHistory
    {
        public bool CorrectChoice { get; set; }

        public bool CorrectTyping { get; set; }
    }
}
