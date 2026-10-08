using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.SupportLinks;

namespace WordBuddy.Identity.UnitTests;

/// <summary>Builders for users and links in a known state.</summary>
internal static class TestData
{
    public static readonly DateTime Now = new(2026, 10, 8, 9, 0, 0, DateTimeKind.Utc);

    public static User Adult(string name = "Adult") =>
        new(Guid.NewGuid(), $"{name.ToLowerInvariant()}@example.com", name, "hash", AgeGroup.Adult, isAdmin: false);

    public static User Child(string name = "Kid") =>
        new(Guid.NewGuid(), $"{name.ToLowerInvariant()}@example.com", name, "hash", AgeGroup.Child, isAdmin: false);

    public static LinkParty Party(User user) => new(user.Id, user.AgeGroup);

    /// <summary>Creates a link through the policy (the only public factory).</summary>
    public static SupportLink Link(User learner, User supporter, IReadOnlyCollection<SupportLink>? existing = null) =>
        SupportLinkPolicy.CreateOnAccept(
            Guid.NewGuid(), Party(learner), Party(supporter), relationship: null, existing ?? [], supporter.Id, Now).Value;
}
