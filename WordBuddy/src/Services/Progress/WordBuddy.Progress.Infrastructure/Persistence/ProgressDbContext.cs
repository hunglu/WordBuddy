using Microsoft.EntityFrameworkCore;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Domain.Challenges;
using WordBuddy.Shared.Infrastructure.Messaging;

namespace WordBuddy.Progress.Infrastructure.Persistence;

/// <summary>EF Core context for the Progress service's own database (<c>WordBuddyProgress</c>).
/// One shared model; the provider subclasses (<c>SqlServerProgressDbContext</c>, <c>PostgresProgressDbContext</c>) own their migrations.</summary>
public abstract class ProgressDbContext : DbContext
{
    /// <summary>Called by the provider subclasses with their own typed options.</summary>
    /// <param name="options">Provider-configured options.</param>
    protected ProgressDbContext(DbContextOptions options) : base(options)
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
    public DbSet<LearnerGroupMemberProjection> LearnerGroupMemberProjections => Set<LearnerGroupMemberProjection>();

    public DbSet<DeletedLearnerGroup> DeletedLearnerGroups => Set<DeletedLearnerGroup>();
    public DbSet<VocabularySessionIssue> VocabularySessionIssues => Set<VocabularySessionIssue>();
    public DbSet<VocabularyExercise> VocabularyExercises => Set<VocabularyExercise>();

    public DbSet<Challenge> Challenges => Set<Challenge>();
    public DbSet<ChallengeParticipant> ChallengeParticipants => Set<ChallengeParticipant>();
    public DbSet<ChallengeProfile> ChallengeProfiles => Set<ChallengeProfile>();
    public DbSet<ChallengeConsent> ChallengeConsents => Set<ChallengeConsent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProgressDbContext).Assembly);
        modelBuilder.AddWordBuddyMessagingEntities();
    }
}
