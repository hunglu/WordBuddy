using Microsoft.EntityFrameworkCore;

namespace WordBuddy.Quiz.Infrastructure.Persistence;

/// <summary>EF Core context for the Quiz service's own database (<c>WordBuddyQuiz</c>).
/// One shared model; the provider subclasses (<c>SqlServerQuizDbContext</c>, <c>PostgresQuizDbContext</c>) own their migrations.</summary>
public abstract class QuizDbContext : DbContext
{
    /// <summary>Called by the provider subclasses with their own typed options.</summary>
    /// <param name="options">Provider-configured options.</param>
    protected QuizDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Domain.Quiz> Quizzes => Set<Domain.Quiz>();
    public DbSet<Domain.QuizQuestion> QuizQuestions => Set<Domain.QuizQuestion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QuizDbContext).Assembly);
    }
}
