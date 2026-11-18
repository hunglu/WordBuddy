using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace WordBuddy.Quiz.Infrastructure.Persistence.Postgres;

/// <summary>
/// PostgreSQL-only changes on top of the shared model: <c>text</c> for the JSON options column and
/// DateTime round-trip parity with SQL Server (<c>datetime2</c>).
/// </summary>
internal static class PostgresQuizModelOverrides
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
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (IMutableProperty property in entityType.GetProperties())
            {
                if (property.FindAnnotation(RelationalAnnotationNames.ColumnType)?.Value as string == "nvarchar(max)")
                {
                    property.SetColumnType("text");
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
