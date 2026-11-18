using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace WordBuddy.Content.Infrastructure.Persistence.Dialects;

/// <summary>SQL Server error numbers: 2601 (unique index), 2627 (unique constraint), 547 (constraint conflict).</summary>
internal sealed class SqlServerDatabaseDialect : IDatabaseDialect
{
    /// <inheritdoc />
    public string GuardedMediaAssetDeleteSql => """
        DELETE FROM [MediaAssets]
        OUTPUT deleted.[Url]
        WHERE [Id] = @id
          AND NOT EXISTS (SELECT 1 FROM [Lexemes] WHERE [UkAudioAssetId] = @id OR [UsAudioAssetId] = @id)
          AND NOT EXISTS (SELECT 1 FROM [Senses] WHERE [AudioAssetId] = @id OR [ImageAssetId] = @id)
          AND NOT EXISTS (SELECT 1 FROM [DailyPhrases] WHERE [AudioAssetId] = @id OR [VideoAssetId] = @id)
        """;

    /// <inheritdoc />
    public bool IsUniqueViolation(Exception exception, string? indexName = null) =>
        Find(exception) is { } db && IsUnique(db.Number, db.Message, indexName);

    /// <inheritdoc />
    public bool IsForeignKeyViolation(Exception exception, string? constraintName = null) =>
        Find(exception) is { } db && IsForeignKey(db.Number, db.Message, constraintName);

    /// <summary>Classifies a SQL Server error number and message (also used by unit tests).</summary>
    internal static bool IsUnique(int number, string message, string? indexName) =>
        number is 2601 or 2627 && (indexName is null || message.Contains(indexName, StringComparison.Ordinal));

    /// <summary>Classifies a SQL Server error number and message (also used by unit tests).</summary>
    internal static bool IsForeignKey(int number, string message, string? constraintName) =>
        number == 547 && (constraintName is null || message.Contains(constraintName, StringComparison.Ordinal));

    private static SqlException? Find(Exception exception) =>
        exception as SqlException ?? exception.InnerException as SqlException;
}
