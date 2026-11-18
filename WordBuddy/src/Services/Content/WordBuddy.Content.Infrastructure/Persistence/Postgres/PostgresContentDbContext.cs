using Microsoft.EntityFrameworkCore;

namespace WordBuddy.Content.Infrastructure.Persistence.Postgres;

/// <summary>PostgreSQL flavour of <see cref="ContentDbContext"/>. Applies <see cref="PostgresContentModelOverrides"/>; owns <c>Migrations/Postgres</c>.</summary>
public sealed class PostgresContentDbContext : ContentDbContext
{
    /// <summary>Creates the context.</summary>
    /// <param name="options">PostgreSQL options.</param>
    public PostgresContentDbContext(DbContextOptions<PostgresContentDbContext> options) : base(options)
    {
    }

    /// <summary>Runtime constructor: <paramref name="serviceProvider"/> resolves the outbox publish endpoint lazily.</summary>
    /// <param name="options">PostgreSQL options.</param>
    /// <param name="serviceProvider">The request scope.</param>
    public PostgresContentDbContext(DbContextOptions<PostgresContentDbContext> options, IServiceProvider serviceProvider) : base(options, serviceProvider)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        PostgresContentModelOverrides.Apply(modelBuilder);
    }
}
