using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="LearnerWordState"/> to <c>LearnerWordStates</c>.</summary>
public sealed class LearnerWordStateConfiguration : IEntityTypeConfiguration<LearnerWordState>
{
    public void Configure(EntityTypeBuilder<LearnerWordState> builder)
    {
        builder.ToTable("LearnerWordStates");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId).IsRequired();
        builder.Property(s => s.SenseId).IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.Stability).IsRequired();
        builder.Property(s => s.Difficulty).IsRequired();
        builder.Property(s => s.DueAtUtc).IsRequired();
        builder.Property(s => s.Reps).IsRequired();
        builder.Property(s => s.Lapses).IsRequired();
        builder.Property(s => s.FsrsPhase).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.FsrsStep);
        builder.Property(s => s.LastReviewedAtUtc);
        builder.Property(s => s.FirstReviewedAtUtc);
        builder.Property(s => s.IsActive).IsRequired();

        // Optimistic concurrency: two reviews that both read the same card cannot both save.
        builder.Property<byte[]>("RowVersion").IsRowVersion();

        // One state per (user, sense) — the learner-word handlers upsert against this.
        builder.HasIndex(s => new { s.UserId, s.SenseId }).IsUnique();

        // Session query: the user's active words by due time.
        builder.HasIndex(s => new { s.UserId, s.IsActive, s.DueAtUtc });
    }
}
