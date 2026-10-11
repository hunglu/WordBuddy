using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Progress.Domain.Challenges;

namespace WordBuddy.Progress.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="ChallengeParticipant"/> to <c>ChallengeParticipants</c>. One row per learner and challenge.</summary>
public sealed class ChallengeParticipantConfiguration : IEntityTypeConfiguration<ChallengeParticipant>
{
    public void Configure(EntityTypeBuilder<ChallengeParticipant> builder)
    {
        builder.ToTable("ChallengeParticipants");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.ChallengeId).IsRequired();
        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.JoinedAtUtc).IsRequired();
        builder.Property(p => p.Score).IsRequired();
        builder.Property(p => p.Rank);
        builder.Property(p => p.TargetReached).IsRequired();

        builder.HasIndex(p => new { p.ChallengeId, p.UserId }).IsUnique();
        builder.HasIndex(p => p.UserId);
    }
}
