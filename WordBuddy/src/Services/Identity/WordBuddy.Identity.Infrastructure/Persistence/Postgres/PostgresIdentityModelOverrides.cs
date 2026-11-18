using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.Groups;
using WordBuddy.Identity.Domain.SupportLinks;

namespace WordBuddy.Identity.Infrastructure.Persistence.Postgres;

/// <summary>
/// PostgreSQL-only changes on top of the shared model: filtered-index syntax, case-insensitive
/// text (<c>citext</c>, to match the SQL Server collation) and DateTime round-trip parity.
/// </summary>
internal static class PostgresIdentityModelOverrides
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

        SetFilter<User>(modelBuilder, "IX_Users_Alias", "\"Alias\" IS NOT NULL");
        SetFilter<UnlinkRequest>(modelBuilder, "IX_UnlinkRequests_LinkId_Open", "\"Status\" IN ('Pending', 'OverrideRequested')");
        SetFilter<SupportLink>(modelBuilder, "IX_SupportLinks_LearnerId_ActivePrimary", "\"IsPrimary\" = TRUE AND \"Status\" = 'Active'");
        SetFilter<LearnerGroupMember>(modelBuilder, "IX_LearnerGroupMembers_GroupId_LearnerId_Open", "\"Status\" <> 'Removed'");

        // Case-insensitive lookups: SQL Server does this with its default collation.
        modelBuilder.Entity<User>().Property(u => u.Email).HasColumnType("citext");
        modelBuilder.Entity<User>().Property(u => u.Alias).HasColumnType("citext");
        modelBuilder.Entity<User>().Property(u => u.DisplayName).HasColumnType("citext");

        ApplyDateTimeConvention(modelBuilder);
    }

    private static void SetFilter<TEntity>(ModelBuilder modelBuilder, string indexName, string filter)
        where TEntity : class
    {
        IMutableIndex index = modelBuilder.Entity<TEntity>().Metadata.GetIndexes()
            .Single(i => i.GetDatabaseName() == indexName);
        index.SetFilter(filter);
    }

    private static void ApplyDateTimeConvention(ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (IMutableProperty property in entityType.GetProperties())
            {
                if (property.FindAnnotation("Relational:ColumnType") is not null)
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
