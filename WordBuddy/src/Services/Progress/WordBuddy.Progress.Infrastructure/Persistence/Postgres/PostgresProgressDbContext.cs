using Microsoft.EntityFrameworkCore;

namespace WordBuddy.Progress.Infrastructure.Persistence.Postgres;

/// <summary>PostgreSQL flavour of <see cref="ProgressDbContext"/>. Applies <see cref="PostgresProgressModelOverrides"/>; owns <c>Migrations/Postgres</c>.</summary>
public sealed class PostgresProgressDbContext : ProgressDbContext
{
    /// <summary>Creates the context.</summary>
    /// <param name="options">PostgreSQL options.</param>
    public PostgresProgressDbContext(DbContextOptions<PostgresProgressDbContext> options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        PostgresProgressModelOverrides.Apply(modelBuilder);
    }
}
