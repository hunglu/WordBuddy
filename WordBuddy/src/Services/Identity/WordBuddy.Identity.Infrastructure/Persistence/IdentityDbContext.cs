using Microsoft.EntityFrameworkCore;
using WordBuddy.Identity.Domain;

namespace WordBuddy.Identity.Infrastructure.Persistence;

/// <summary>EF Core context for the Identity service's own database (<c>WordBuddyIdentity</c>).</summary>
public sealed class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}
