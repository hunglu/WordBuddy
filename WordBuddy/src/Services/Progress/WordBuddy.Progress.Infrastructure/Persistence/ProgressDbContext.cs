using Microsoft.EntityFrameworkCore;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Infrastructure.Messaging;

namespace WordBuddy.Progress.Infrastructure.Persistence;

/// <summary>EF Core context for the Progress service's own database (<c>WordBuddyProgress</c>).</summary>
public sealed class ProgressDbContext : DbContext
{
    public ProgressDbContext(DbContextOptions<ProgressDbContext> options) : base(options)
    {
    }

    public DbSet<LearnerProgress> LearnerProgressEntries => Set<LearnerProgress>();
    public DbSet<VocabularyRecallStat> VocabularyRecallStats => Set<VocabularyRecallStat>();
    public DbSet<VocabularyRecallSession> VocabularyRecallSessions => Set<VocabularyRecallSession>();
    public DbSet<LearnerWordMembership> LearnerWordMemberships => Set<LearnerWordMembership>();
    public DbSet<LearnerWordState> LearnerWordStates => Set<LearnerWordState>();
    public DbSet<ReviewLog> ReviewLogs => Set<ReviewLog>();
    public DbSet<VocabularyLearnerSettings> VocabularyLearnerSettings => Set<VocabularyLearnerSettings>();
    public DbSet<SupportLinkProjection> SupportLinkProjections => Set<SupportLinkProjection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProgressDbContext).Assembly);
        modelBuilder.AddWordBuddyMessagingEntities();
    }
}
