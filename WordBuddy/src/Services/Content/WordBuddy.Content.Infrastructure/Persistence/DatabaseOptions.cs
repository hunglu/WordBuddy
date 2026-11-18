using Microsoft.Extensions.Configuration;

namespace WordBuddy.Content.Infrastructure.Persistence;

/// <summary>Database engines the Content service can run on.</summary>
public enum DatabaseProvider
{
    /// <summary>SQL Server (default).</summary>
    SqlServer = 0,

    /// <summary>PostgreSQL 15 or newer.</summary>
    Postgres = 1,
}

/// <summary>
/// Selects the persistence provider. Config key <c>Database:Provider</c>: <c>SqlServer</c> (default
/// when missing) or <c>Postgres</c>. Any other value fails at startup.
/// </summary>
public sealed class DatabaseOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Database";

    /// <summary>The selected provider.</summary>
    public DatabaseProvider Provider { get; init; } = DatabaseProvider.SqlServer;

    /// <summary>Reads and validates the section once. Throws <see cref="InvalidOperationException"/> for an unknown value.</summary>
    /// <param name="configuration">The application configuration.</param>
    public static DatabaseOptions FromConfiguration(IConfiguration configuration)
    {
        string? raw = configuration.GetSection(SectionName)["Provider"];
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new DatabaseOptions();
        }

        string value = raw.Trim();
        if (string.Equals(value, nameof(DatabaseProvider.SqlServer), StringComparison.OrdinalIgnoreCase))
        {
            return new DatabaseOptions { Provider = DatabaseProvider.SqlServer };
        }

        if (string.Equals(value, nameof(DatabaseProvider.Postgres), StringComparison.OrdinalIgnoreCase))
        {
            return new DatabaseOptions { Provider = DatabaseProvider.Postgres };
        }

        throw new InvalidOperationException(
            $"Unknown '{SectionName}:Provider' value '{raw}'. Use 'SqlServer' or 'Postgres'.");
    }
}
