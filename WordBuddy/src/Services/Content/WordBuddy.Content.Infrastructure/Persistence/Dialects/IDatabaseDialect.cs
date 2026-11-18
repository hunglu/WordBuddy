namespace WordBuddy.Content.Infrastructure.Persistence.Dialects;

/// <summary>
/// The few places where repositories must know the database engine: error classification and one
/// raw SQL statement. Registered once per provider.
/// </summary>
internal interface IDatabaseDialect
{
    /// <summary>
    /// The statement that deletes a <c>MediaAssets</c> row (parameter <c>@id</c>) only while no
    /// <c>Lexemes</c>/<c>Senses</c>/<c>DailyPhrases</c> row references it, and returns the deleted <c>Url</c>
    /// (no row = nothing deleted).
    /// </summary>
    string GuardedMediaAssetDeleteSql { get; }

    /// <summary>True when <paramref name="exception"/> (or its inner exception) is a duplicate-key error.</summary>
    /// <param name="exception">The caught exception, usually a <c>DbUpdateException</c>.</param>
    /// <param name="indexName">When set, the violated index or constraint must have this name.</param>
    bool IsUniqueViolation(Exception exception, string? indexName = null);

    /// <summary>True when <paramref name="exception"/> (or its inner exception) is a foreign-key error.</summary>
    /// <param name="exception">The caught exception.</param>
    /// <param name="constraintName">When set, the violated constraint must have this name.</param>
    bool IsForeignKeyViolation(Exception exception, string? constraintName = null);
}
