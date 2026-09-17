using Microsoft.EntityFrameworkCore;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.Infrastructure.Persistence;

/// <summary>EF Core context for the Progress service's own database (<c>WordBuddyProgress</c>).</summary>
public sealed class ProgressDbContext : DbContext
{
    public ProgressDbContext(DbContextOptions<ProgressDbContext> options) : base(options)
    {
    }

    public DbSet<LearnerProgress> LearnerProgressEntries => Set<LearnerProgress>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProgressDbContext).Assembly);
    }
}
