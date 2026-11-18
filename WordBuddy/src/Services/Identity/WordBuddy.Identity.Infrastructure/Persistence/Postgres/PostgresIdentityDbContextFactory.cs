using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WordBuddy.Identity.Infrastructure.Persistence.Postgres;

/// <summary>Lets <c>dotnet ef</c> create a <see cref="PostgresIdentityDbContext"/> at design time. <c>migrations add</c> does not connect, so no password is set.</summary>
internal sealed class PostgresIdentityDbContextFactory : IDesignTimeDbContextFactory<PostgresIdentityDbContext>
{
    public PostgresIdentityDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<PostgresIdentityDbContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql("Host=localhost;Database=WordBuddyIdentity");
        return new PostgresIdentityDbContext(optionsBuilder.Options);
    }
}
