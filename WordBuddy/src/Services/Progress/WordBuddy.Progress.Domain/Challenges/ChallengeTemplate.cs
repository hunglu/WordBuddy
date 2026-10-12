namespace WordBuddy.Progress.Domain.Challenges;

/// <summary>One UTC window of a challenge instance.</summary>
/// <param name="OpensAtUtc">Joining opens.</param>
/// <param name="StartsAtUtc">Counting starts.</param>
/// <param name="EndsAtUtc">Counting ends (exclusive).</param>
public sealed record ChallengeWindow(DateTime OpensAtUtc, DateTime StartsAtUtc, DateTime EndsAtUtc);

/// <summary>A launch challenge template. Instances are created by the scheduler, never by users.</summary>
/// <param name="Key">Stable key stored on instances.</param>
/// <param name="Name">Display name.</param>
/// <param name="Kind">Ranked or target.</param>
/// <param name="Metric">What is counted.</param>
/// <param name="Target">Target to reach (target kind), else null.</param>
/// <param name="Period">Length of one instance. Weekly templates start on Monday 00:00 UTC.</param>
/// <param name="StartsOnMonday">Whether instances are aligned to calendar weeks.</param>
/// <param name="Audiences">Audiences that get an instance. Never mixed in one instance.</param>
public sealed record ChallengeTemplate(
    string Key,
    string Name,
    ChallengeKind Kind,
    ChallengeMetric Metric,
    int? Target,
    TimeSpan Period,
    bool StartsOnMonday,
    IReadOnlyList<ChallengeAudience> Audiences)
{
    private const int MaxCatchUpWindows = 4;

    /// <summary>
    /// The windows to create now. No instance yet: the period that contains <paramref name="nowUtc"/>.
    /// Otherwise, once the latest instance has started, the follow-up windows (contiguous) until one starts in the future;
    /// whole periods that are already over are skipped.
    /// Never creates more than one future window, so there is no overflow instance.
    /// </summary>
    public IReadOnlyList<ChallengeWindow> NextWindows(DateTime? latestStartsAtUtc, DateTime? latestEndsAtUtc, DateTime nowUtc, TimeSpan openLead)
    {
        List<ChallengeWindow> windows = [];
        DateTime cursor;
        if (latestStartsAtUtc is null || latestEndsAtUtc is null)
        {
            cursor = FirstStart(nowUtc);
        }
        else if (latestStartsAtUtc.Value > nowUtc)
        {
            return windows;
        }
        else
        {
            cursor = latestEndsAtUtc.Value;

            // The scheduler was down: skip whole periods that are already over (nobody could join them)
            // and keep the alignment (multiples of the period).
            if (cursor + Period <= nowUtc)
            {
                long missed = (long)((nowUtc - cursor) / Period);
                cursor += missed * Period;
            }
        }

        while (windows.Count < MaxCatchUpWindows)
        {
            ChallengeWindow window = new(cursor - openLead, cursor, cursor + Period);
            windows.Add(window);
            if (window.StartsAtUtc > nowUtc)
            {
                break;
            }

            cursor = window.EndsAtUtc;
        }

        return windows;
    }

    private DateTime FirstStart(DateTime nowUtc)
    {
        DateTime today = DateTime.SpecifyKind(nowUtc.Date, DateTimeKind.Utc);
        if (!StartsOnMonday)
        {
            return today;
        }

        int sinceMonday = ((int)today.DayOfWeek + 6) % 7;
        return today.AddDays(-sinceMonday);
    }
}
