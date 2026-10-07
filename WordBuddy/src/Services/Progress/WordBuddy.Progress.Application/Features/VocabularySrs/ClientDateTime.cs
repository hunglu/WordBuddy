using System.Globalization;
using System.Text.RegularExpressions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.VocabularySrs;

/// <summary>
/// Parses the <c>X-Client-CurrentDateTime</c> header (D-2). Only the offset is used — to find the
/// client's "today". Due checks and stored times always use server UTC.
/// </summary>
public static partial class ClientDateTime
{
    /// <summary>Header name.</summary>
    public const string HeaderName = "X-Client-CurrentDateTime";

    private static readonly TimeSpan MinOffset = TimeSpan.FromHours(-12);
    private static readonly TimeSpan MaxOffset = TimeSpan.FromHours(14);
    private static readonly TimeSpan MaxSkew = TimeSpan.FromHours(24);

    /// <summary>
    /// Returns the client's UTC offset. Missing header → <see cref="TimeSpan.Zero"/>. Bad format,
    /// offset outside −12:00…+14:00, or more than 24 h from <paramref name="nowUtc"/> → validation error.
    /// </summary>
    public static Result<TimeSpan> ParseOffset(string? header, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return Result.Success(TimeSpan.Zero);
        }

        if (!IsoWithOffset().IsMatch(header)
            || !DateTimeOffset.TryParse(header, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset clientNow))
        {
            return Invalid("must be an ISO 8601 date-time with offset, e.g. 2026-10-07T09:30:00+07:00");
        }

        if (clientNow.Offset < MinOffset || clientNow.Offset > MaxOffset)
        {
            return Invalid("offset must be between -12:00 and +14:00");
        }

        if ((clientNow.UtcDateTime - nowUtc).Duration() > MaxSkew)
        {
            return Invalid("must be within 24 hours of server time");
        }

        return Result.Success(clientNow.Offset);
    }

    /// <summary>Returns the UTC range [start, end) of the client's current day.</summary>
    public static (DateTime StartUtc, DateTime EndUtc) TodayUtcRange(DateTime nowUtc, TimeSpan offset)
    {
        DateTime localDate = (nowUtc + offset).Date;
        DateTime startUtc = DateTime.SpecifyKind(localDate - offset, DateTimeKind.Utc);
        return (startUtc, startUtc.AddDays(1));
    }

    private static Result<TimeSpan> Invalid(string reason) =>
        Result.Failure<TimeSpan>(Error.Validation("ClientDateTime.Invalid", $"{HeaderName} {reason}."));

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(:\d{2}(\.\d{1,7})?)?(Z|[+-]\d{2}:\d{2})$")]
    private static partial Regex IsoWithOffset();
}
