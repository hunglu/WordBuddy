using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WordBuddy.Progress.Infrastructure.Persistence.Postgres;

/// <summary>Lets <c>dotnet ef</c> create a <see cref="PostgresProgressDbContext"/> at design time. <c>migrations add</c> does not connect, so no password is set.</summary>
internal sealed class PostgresProgressDbContextFactory : IDesignTimeDbContextFactory<PostgresProgressDbContext>
{
    public PostgresProgressDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<PostgresProgressDbContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql("Host=localhost;Database=WordBuddyProgress");
        return new PostgresProgressDbContext(optionsBuilder.Options);
    }
}
