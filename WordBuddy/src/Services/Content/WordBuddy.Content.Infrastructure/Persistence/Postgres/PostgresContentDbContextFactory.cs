using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WordBuddy.Content.Infrastructure.Persistence.Postgres;

/// <summary>Lets <c>dotnet ef</c> create a <see cref="PostgresContentDbContext"/> at design time. <c>migrations add</c> does not connect, so no password is set.</summary>
internal sealed class PostgresContentDbContextFactory : IDesignTimeDbContextFactory<PostgresContentDbContext>
{
    public PostgresContentDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<PostgresContentDbContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql("Host=localhost;Database=WordBuddyContent");
        return new PostgresContentDbContext(optionsBuilder.Options);
    }
}
