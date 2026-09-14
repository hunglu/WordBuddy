using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WordBuddy.Content.Infrastructure.Persistence;

/// <summary>Lets <c>dotnet ef</c> create a <see cref="ContentDbContext"/> at design time without starting the API host.</summary>
internal sealed class ContentDbContextFactory : IDesignTimeDbContextFactory<ContentDbContext>
{
    public ContentDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<ContentDbContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=WordBuddyContent;Trusted_Connection=true");
        return new ContentDbContext(optionsBuilder.Options);
    }
}
