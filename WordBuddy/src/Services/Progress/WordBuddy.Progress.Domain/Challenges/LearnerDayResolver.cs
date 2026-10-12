using System.Collections.Concurrent;

namespace WordBuddy.Progress.Domain.Challenges;

/// <summary>Turns a UTC instant into the learner local calendar day. Missing or unknown zone: UTC.</summary>
public static class LearnerDayResolver
{
    private static readonly ConcurrentDictionary<string, TimeZoneInfo?> Zones = new(StringComparer.Ordinal);

    /// <summary>The local day of <paramref name="utc"/> in <paramref name="timeZoneId"/> (IANA id).</summary>
    public static DateOnly ToLocalDay(DateTime utc, string? timeZoneId)
    {
        DateTime asUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        TimeZoneInfo? zone = Find(timeZoneId);
        return DateOnly.FromDateTime(zone is null ? asUtc : TimeZoneInfo.ConvertTimeFromUtc(asUtc, zone));
    }

    private static TimeZoneInfo? Find(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return null;
        }

        return Zones.GetOrAdd(timeZoneId.Trim(), static id =>
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                return null;
            }
            catch (InvalidTimeZoneException)
            {
                return null;
            }
        });
    }
}
