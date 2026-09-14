using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WordBuddy.Identity.Infrastructure.Persistence;

/// <summary>Lets <c>dotnet ef</c> create an <see cref="IdentityDbContext"/> at design time without starting the API host.</summary>
internal sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<IdentityDbContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=WordBuddyIdentity;Trusted_Connection=true");
        return new IdentityDbContext(optionsBuilder.Options);
    }
}
