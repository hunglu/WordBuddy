using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WordBuddy.Quiz.Infrastructure.Persistence.SqlServer;

/// <summary>Lets <c>dotnet ef</c> create a <see cref="SqlServerQuizDbContext"/> at design time without starting the API host.</summary>
internal sealed class SqlServerQuizDbContextFactory : IDesignTimeDbContextFactory<SqlServerQuizDbContext>
{
    public SqlServerQuizDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<SqlServerQuizDbContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=WordBuddyQuiz;Trusted_Connection=true");
        return new SqlServerQuizDbContext(optionsBuilder.Options);
    }
}
