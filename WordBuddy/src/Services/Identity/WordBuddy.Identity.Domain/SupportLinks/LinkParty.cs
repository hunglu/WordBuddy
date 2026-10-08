namespace WordBuddy.Identity.Domain.SupportLinks;

/// <summary>The id and age group of a user taking part in a link decision.</summary>
public sealed record LinkParty(Guid UserId, AgeGroup AgeGroup)
{
    /// <summary>Gets a value indicating whether the party is a child account.</summary>
    public bool IsChild => AgeGroup == AgeGroup.Child;
}
