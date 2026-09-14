using Microsoft.EntityFrameworkCore;

namespace WordBuddy.Quiz.Infrastructure.Persistence;

/// <summary>EF Core context for the Quiz service's own database (<c>WordBuddyQuiz</c>).</summary>
public sealed class QuizDbContext : DbContext
{
    public QuizDbContext(DbContextOptions<QuizDbContext> options) : base(options)
    {
    }

    public DbSet<Domain.Quiz> Quizzes => Set<Domain.Quiz>();
    public DbSet<Domain.QuizQuestion> QuizQuestions => Set<Domain.QuizQuestion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QuizDbContext).Assembly);
    }
}
