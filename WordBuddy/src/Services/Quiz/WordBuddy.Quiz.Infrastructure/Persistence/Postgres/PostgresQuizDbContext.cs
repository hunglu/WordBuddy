using Microsoft.EntityFrameworkCore;

namespace WordBuddy.Quiz.Infrastructure.Persistence.Postgres;

/// <summary>PostgreSQL flavour of <see cref="QuizDbContext"/>. Applies <see cref="PostgresQuizModelOverrides"/>; owns <c>Migrations/Postgres</c>.</summary>
public sealed class PostgresQuizDbContext : QuizDbContext
{
    /// <summary>Creates the context.</summary>
    /// <param name="options">PostgreSQL options.</param>
    public PostgresQuizDbContext(DbContextOptions<PostgresQuizDbContext> options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        PostgresQuizModelOverrides.Apply(modelBuilder);
    }
}
