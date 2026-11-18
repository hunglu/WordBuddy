using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WordBuddy.Progress.Infrastructure.Persistence.SqlServer;

/// <summary>Lets <c>dotnet ef</c> create a <see cref="SqlServerProgressDbContext"/> at design time without starting the API host.</summary>
internal sealed class SqlServerProgressDbContextFactory : IDesignTimeDbContextFactory<SqlServerProgressDbContext>
{
    public SqlServerProgressDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<SqlServerProgressDbContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=WordBuddyProgress;Trusted_Connection=true");
        return new SqlServerProgressDbContext(optionsBuilder.Options);
    }
}
