using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.Infrastructure.Persistence.Postgres;

/// <summary>
/// PostgreSQL-only changes on top of the shared model: filtered-index syntax, the <c>xmin</c> system column as
/// optimistic-concurrency token (instead of the SQL Server <c>rowversion</c>) and DateTime round-trip parity.
/// </summary>
internal static class PostgresProgressModelOverrides
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
        // One answer per exercise: the index only covers rows that have an exercise id.
        IMutableIndex exerciseIndex = modelBuilder.Entity<ReviewLog>().Metadata.GetIndexes()
            .Single(i => i.Properties.Count == 1 && i.Properties[0].Name == nameof(ReviewLog.ExerciseId));
        exerciseIndex.SetFilter("\"ExerciseId\" IS NOT NULL");

        // SQL Server uses a rowversion column. PostgreSQL has none; its xmin system column changes on every update.
        IMutableEntityType wordState = modelBuilder.Entity<LearnerWordState>().Metadata;
        wordState.RemoveProperty("RowVersion");
        modelBuilder.Entity<LearnerWordState>().Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (IMutableProperty property in entityType.GetProperties())
            {
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
