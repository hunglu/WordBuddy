using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WordBuddy.Content.Domain;
using WordBuddy.Content.Infrastructure.Persistence.Configurations;

namespace WordBuddy.Content.Infrastructure.Persistence.Postgres;

/// <summary>
/// PostgreSQL-only changes on top of the shared model: filtered-index syntax, NULL semantics of the
/// lexeme unique index (SQL Server treats NULLs as equal), <c>text</c> for JSON columns,
/// case-insensitive <c>Senses.Word</c> (<c>citext</c>) and DateTime round-trip parity.
/// </summary>
internal static class PostgresContentModelOverrides
{
    private static readonly ValueConverter<DateTime, DateTime> UtcConverter = new(
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc),
        v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified));

    private static readonly ValueConverter<DateTime?, DateTime?> NullableUtcConverter = new(
        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v,
        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Unspecified) : v);

    /// <summary>Applies the overrides. Call after the shared model is built.</summary>
    /// <param name="modelBuilder">The model builder.</param>
    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");

        // Learner-only uniqueness (system senses may repeat).
        IMutableIndex learnerIndex = modelBuilder.Entity<Sense>().Metadata.GetIndexes()
            .Single(i => i.GetDatabaseName() == SenseConfiguration.ContentHashOwnerIndexName);
        learnerIndex.SetFilter("\"Source\" = 'Learner'");

        // (NormalizedLemma, PartOfSpeech) must treat two NULL parts of speech as equal, as SQL Server does.
        IMutableIndex lexemeIndex = modelBuilder.Entity<Lexeme>().Metadata.GetIndexes()
            .Single(i => i.GetDatabaseName() == LexemeConfiguration.LemmaPartOfSpeechIndexName);
        lexemeIndex.SetFilter(null);
        lexemeIndex.SetAreNullsDistinct(false);

        // Case-insensitive personal vocabulary search: SQL Server does this with its default collation.
        modelBuilder.Entity<Sense>().Property(s => s.Word).HasColumnType("citext");

        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (IMutableProperty property in entityType.GetProperties())
            {
                if (property.FindAnnotation(RelationalAnnotationNames.ColumnType)?.Value as string == "nvarchar(max)")
                {
                    property.SetColumnType("text");
                }

                if (property.GetDefaultValueSql() == "N'[]'")
                {
                    property.SetDefaultValueSql("'[]'");
                }

                if (property.FindAnnotation(RelationalAnnotationNames.ColumnType) is not null)
                {
                    continue;
                }

                if (property.ClrType == typeof(DateTime))
                {
                    property.SetColumnType("timestamp with time zone");
                    property.SetValueConverter(UtcConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetColumnType("timestamp with time zone");
                    property.SetValueConverter(NullableUtcConverter);
                }
            }
        }
    }
}
