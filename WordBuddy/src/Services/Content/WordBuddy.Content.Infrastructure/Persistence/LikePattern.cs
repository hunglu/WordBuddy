namespace WordBuddy.Content.Infrastructure.Persistence;

/// <summary>Helper for SQL <c>LIKE</c> patterns that use <c>\</c> as the escape character.</summary>
internal static class LikePattern
{
    /// <summary>The escape character to pass to <c>EF.Functions.Like(..., escapeChar)</c>.</summary>
    public const string EscapeCharacter = "\\";

    /// <summary>Escapes <paramref name="term"/> so that it matches literally inside a <c>LIKE</c>
    /// pattern: the escape character <c>\</c> first, then <c>%</c>, <c>_</c> and <c>[</c>.</summary>
    public static string Escape(string term) =>
        term
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal);
}
