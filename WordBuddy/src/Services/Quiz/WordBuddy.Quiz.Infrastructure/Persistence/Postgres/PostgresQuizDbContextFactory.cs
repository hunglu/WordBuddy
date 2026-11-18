using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WordBuddy.Quiz.Infrastructure.Persistence.Postgres;

/// <summary>Lets <c>dotnet ef</c> create a <see cref="PostgresQuizDbContext"/> at design time. <c>migrations add</c> does not connect, so no password is set.</summary>
internal sealed class PostgresQuizDbContextFactory : IDesignTimeDbContextFactory<PostgresQuizDbContext>
{
    public PostgresQuizDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<PostgresQuizDbContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql("Host=localhost;Database=WordBuddyQuiz");
        return new PostgresQuizDbContext(optionsBuilder.Options);
    }
}
