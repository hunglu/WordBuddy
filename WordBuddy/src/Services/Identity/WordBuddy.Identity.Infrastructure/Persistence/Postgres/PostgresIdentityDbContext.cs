using Microsoft.EntityFrameworkCore;

namespace WordBuddy.Identity.Infrastructure.Persistence.Postgres;

/// <summary>PostgreSQL flavour of <see cref="IdentityDbContext"/>. Applies <see cref="PostgresIdentityModelOverrides"/>; owns <c>Migrations/Postgres</c>.</summary>
public sealed class PostgresIdentityDbContext : IdentityDbContext
{
    /// <summary>Creates the context.</summary>
    /// <param name="options">PostgreSQL options.</param>
    public PostgresIdentityDbContext(DbContextOptions<PostgresIdentityDbContext> options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        PostgresIdentityModelOverrides.Apply(modelBuilder);
    }
}
