namespace WordBuddy.Progress.Domain;

/// <summary>Response time chosen for grading.</summary>
/// <param name="UsedMs">Value used for grading.</param>
/// <param name="ClientMs">Value the client sent.</param>
/// <param name="ServerMs">Server-measured time from issue to answer.</param>
/// <param name="Adjusted"><see langword="true"/> when the client value was rejected.</param>
public readonly record struct ResponseTiming(int UsedMs, int ClientMs, int ServerMs, bool Adjusted);

/// <summary>
/// Compares the client timer with the server clock. The client value is accepted when
/// <c>0 ≤ client ≤ server</c> and <c>server − client ≤ ToleranceMs</c>. Otherwise
/// <c>server − ToleranceMs</c> (floor 0) is used. Pure.
/// </summary>
public sealed class ResponseTimeEvaluator
{
    private readonly int _toleranceMs;

    /// <summary>Creates an evaluator with the configured tolerance.</summary>
    public ResponseTimeEvaluator(VocabularyGradingOptions options)
    {
        _toleranceMs = options.ToleranceMs;
    }

    /// <summary>Picks the response time for one answer.</summary>
    public ResponseTiming Evaluate(int clientMs, DateTime issuedAtUtc, DateTime answeredAtUtc)
    {
        long serverLong = Math.Max(0L, (long)(answeredAtUtc - issuedAtUtc).TotalMilliseconds);
        int serverMs = (int)Math.Min(serverLong, int.MaxValue);

        bool accepted = clientMs >= 0 && clientMs <= serverMs && (long)serverMs - clientMs <= _toleranceMs;
        if (accepted)
        {
            return new ResponseTiming(clientMs, clientMs, serverMs, false);
        }

        int used = (int)Math.Max(0L, (long)serverMs - _toleranceMs);
        return new ResponseTiming(used, clientMs, serverMs, true);
    }
}
