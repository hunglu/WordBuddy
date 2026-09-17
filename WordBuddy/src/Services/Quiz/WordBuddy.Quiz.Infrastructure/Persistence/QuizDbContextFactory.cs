using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WordBuddy.Quiz.Infrastructure.Persistence;

/// <summary>Lets <c>dotnet ef</c> create a <see cref="QuizDbContext"/> at design time without starting the API host.</summary>
internal sealed class QuizDbContextFactory : IDesignTimeDbContextFactory<QuizDbContext>
{
    public QuizDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<QuizDbContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=WordBuddyQuiz;Trusted_Connection=true");
        return new QuizDbContext(optionsBuilder.Options);
    }
}
