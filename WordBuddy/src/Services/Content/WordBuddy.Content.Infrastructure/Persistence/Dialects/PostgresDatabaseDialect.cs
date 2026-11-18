using Npgsql;

namespace WordBuddy.Content.Infrastructure.Persistence.Dialects;

/// <summary>PostgreSQL SQLSTATE codes: 23505 (unique_violation), 23503 (foreign_key_violation).</summary>
internal sealed class PostgresDatabaseDialect : IDatabaseDialect
{
    /// <inheritdoc />
    public string GuardedMediaAssetDeleteSql => """
        DELETE FROM "MediaAssets"
        WHERE "Id" = @id
          AND NOT EXISTS (SELECT 1 FROM "Lexemes" WHERE "UkAudioAssetId" = @id OR "UsAudioAssetId" = @id)
          AND NOT EXISTS (SELECT 1 FROM "Senses" WHERE "AudioAssetId" = @id OR "ImageAssetId" = @id)
          AND NOT EXISTS (SELECT 1 FROM "DailyPhrases" WHERE "AudioAssetId" = @id OR "VideoAssetId" = @id)
        RETURNING "Url"
        """;

    /// <inheritdoc />
    public bool IsUniqueViolation(Exception exception, string? indexName = null) =>
        Find(exception) is { } pg && IsUnique(pg.SqlState, pg.ConstraintName, indexName);

    /// <inheritdoc />
    public bool IsForeignKeyViolation(Exception exception, string? constraintName = null) =>
        Find(exception) is { } pg && IsForeignKey(pg.SqlState, pg.ConstraintName, constraintName);

    /// <summary>Classifies a SQLSTATE and constraint name (also used by unit tests).</summary>
    internal static bool IsUnique(string? sqlState, string? actualName, string? expectedName) =>
        sqlState == PostgresErrorCodes.UniqueViolation && (expectedName is null || actualName == expectedName);

    /// <summary>Classifies a SQLSTATE and constraint name (also used by unit tests).</summary>
    internal static bool IsForeignKey(string? sqlState, string? actualName, string? expectedName) =>
        sqlState == PostgresErrorCodes.ForeignKeyViolation && (expectedName is null || actualName == expectedName);

    private static PostgresException? Find(Exception exception) =>
        exception as PostgresException ?? exception.InnerException as PostgresException;
}
