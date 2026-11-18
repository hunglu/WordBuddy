using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WordBuddy.Identity.Infrastructure.Persistence.SqlServer;

/// <summary>Lets <c>dotnet ef</c> create a <see cref="SqlServerIdentityDbContext"/> at design time without starting the API host.</summary>
internal sealed class SqlServerIdentityDbContextFactory : IDesignTimeDbContextFactory<SqlServerIdentityDbContext>
{
    public SqlServerIdentityDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<SqlServerIdentityDbContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=WordBuddyIdentity;Trusted_Connection=true");
        return new SqlServerIdentityDbContext(optionsBuilder.Options);
    }
}
