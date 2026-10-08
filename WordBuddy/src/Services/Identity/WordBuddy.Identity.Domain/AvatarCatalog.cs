namespace WordBuddy.Identity.Domain;

/// <summary>Fixed catalog of avatar ids. No uploads, so no child photos are ever stored.</summary>
public static class AvatarCatalog
{
    /// <summary>All valid avatar ids.</summary>
    public static readonly IReadOnlyList<string> Ids =
        ["fox", "owl", "cat", "dog", "panda", "lion", "rabbit", "turtle", "robot", "rocket"];

    /// <summary>Whether <paramref name="avatarId"/> is in the catalog.</summary>
    public static bool Contains(string avatarId) => Ids.Contains(avatarId, StringComparer.Ordinal);
}
