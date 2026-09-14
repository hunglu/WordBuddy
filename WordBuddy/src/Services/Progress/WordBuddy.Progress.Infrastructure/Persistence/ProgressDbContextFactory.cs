using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WordBuddy.Progress.Infrastructure.Persistence;

/// <summary>Lets <c>dotnet ef</c> create a <see cref="ProgressDbContext"/> at design time without starting the API host.</summary>
internal sealed class ProgressDbContextFactory : IDesignTimeDbContextFactory<ProgressDbContext>
{
    public ProgressDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<ProgressDbContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=WordBuddyProgress;Trusted_Connection=true");
        return new ProgressDbContext(optionsBuilder.Options);
    }
}
