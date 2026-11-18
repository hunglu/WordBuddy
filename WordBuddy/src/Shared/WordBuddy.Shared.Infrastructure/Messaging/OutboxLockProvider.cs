namespace WordBuddy.Shared.Infrastructure.Messaging;

/// <summary>
/// Database flavour the MassTransit EF Core outbox uses for its row-lock statements.
/// Must match the provider of the service DbContext.
/// </summary>
public enum OutboxLockProvider
{
    /// <summary>SQL Server lock hints (default).</summary>
    SqlServer = 0,

    /// <summary>PostgreSQL <c>FOR UPDATE</c> locks.</summary>
    Postgres = 1,
}
