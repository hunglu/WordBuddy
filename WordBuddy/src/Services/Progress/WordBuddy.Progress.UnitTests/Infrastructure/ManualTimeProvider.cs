namespace WordBuddy.Progress.UnitTests.Infrastructure;

/// <summary>A <see cref="TimeProvider"/> whose clock only moves when a test advances it.</summary>
internal sealed class ManualTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public ManualTimeProvider(DateTimeOffset start)
    {
        _utcNow = start;
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Advance(TimeSpan by) => _utcNow += by;
}
