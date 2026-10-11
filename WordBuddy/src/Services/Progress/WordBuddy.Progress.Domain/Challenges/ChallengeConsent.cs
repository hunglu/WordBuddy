namespace WordBuddy.Progress.Domain.Challenges;

/// <summary>A Guardian allowed challenges for one child. Removed when the link or the role ends.</summary>
public sealed class ChallengeConsent
{
    /// <summary>The child (plain field).</summary>
    public Guid ChildId { get; private set; }

    /// <summary>The guardian who gave the consent (plain field).</summary>
    public Guid EnabledBy { get; private set; }

    /// <summary>When the consent was given, UTC.</summary>
    public DateTime EnabledAtUtc { get; private set; }

    private ChallengeConsent()
    {
    }

    /// <summary>Creates a consent.</summary>
    public static ChallengeConsent Create(Guid childId, Guid enabledBy, DateTime enabledAtUtc) =>
        new() { ChildId = childId, EnabledBy = enabledBy, EnabledAtUtc = enabledAtUtc };
}
