using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WordBuddy.Infrastructure.Persistence;

/// <summary>
/// Creates a <see cref="WordBuddyDbContext"/> for EF Core design-time operations (migrations).
/// Prevents <c>dotnet ef</c> from needing to fully start the API host.
/// </summary>
internal sealed class WordBuddyDbContextFactory : IDesignTimeDbContextFactory<WordBuddyDbContext>
{
    /// <inheritdoc />
    public WordBuddyDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<WordBuddyDbContext> options = new();
        options.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=WordBuddy;Trusted_Connection=true");
        return new WordBuddyDbContext(options.Options);
    }
}
