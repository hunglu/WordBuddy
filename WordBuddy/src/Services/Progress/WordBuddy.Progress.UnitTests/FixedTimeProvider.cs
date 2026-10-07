namespace WordBuddy.Progress.UnitTests;

/// <summary>A <see cref="TimeProvider"/> that always returns <see cref="UtcNow"/>.</summary>
internal sealed class FixedTimeProvider : TimeProvider
{
    public FixedTimeProvider(DateTime utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTime UtcNow { get; set; }

    public override DateTimeOffset GetUtcNow() => new(UtcNow, TimeSpan.Zero);
}
