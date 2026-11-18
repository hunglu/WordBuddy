using Microsoft.EntityFrameworkCore;

namespace WordBuddy.Quiz.Infrastructure.Persistence.SqlServer;

/// <summary>SQL Server flavour of <see cref="QuizDbContext"/>. Adds nothing to the model; owns <c>Migrations/SqlServer</c>.</summary>
public sealed class SqlServerQuizDbContext : QuizDbContext
{
    /// <summary>Creates the context.</summary>
    /// <param name="options">SQL Server options.</param>
    public SqlServerQuizDbContext(DbContextOptions<SqlServerQuizDbContext> options) : base(options)
    {
    }
}
