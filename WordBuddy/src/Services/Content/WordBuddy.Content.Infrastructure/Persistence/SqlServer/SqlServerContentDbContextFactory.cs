using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WordBuddy.Content.Infrastructure.Persistence.SqlServer;

/// <summary>Lets <c>dotnet ef</c> create a <see cref="SqlServerContentDbContext"/> at design time without starting the API host.</summary>
internal sealed class SqlServerContentDbContextFactory : IDesignTimeDbContextFactory<SqlServerContentDbContext>
{
    public SqlServerContentDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<SqlServerContentDbContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=WordBuddyContent;Trusted_Connection=true");
        return new SqlServerContentDbContext(optionsBuilder.Options);
    }
}
