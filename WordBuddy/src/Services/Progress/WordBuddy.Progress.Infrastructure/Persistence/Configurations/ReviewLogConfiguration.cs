using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.Infrastructure.Persistence.Configurations;

/// <summary>Maps the insert-only <see cref="ReviewLog"/> to <c>ReviewLogs</c>.</summary>
public sealed class ReviewLogConfiguration : IEntityTypeConfiguration<ReviewLog>
{
    public void Configure(EntityTypeBuilder<ReviewLog> builder)
    {
        builder.ToTable("ReviewLogs");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.UserId).IsRequired();
        builder.Property(l => l.SenseId).IsRequired();
        builder.Property(l => l.SessionId).IsRequired();
        builder.Property(l => l.OccurredAtUtc).IsRequired();
        builder.Property(l => l.ExerciseType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(l => l.Skill).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(l => l.IsCorrect).IsRequired();
        builder.Property(l => l.ResponseMs).IsRequired();
        builder.Property(l => l.HintUsed).IsRequired();
        builder.Property(l => l.IsDue).IsRequired();
        builder.Property(l => l.AttemptNo).IsRequired();
        builder.Property(l => l.Rating).HasConversion<string>().HasMaxLength(10).IsRequired();

        // WB-28: nullable so rows written before server-side checking stay valid.
        builder.Property(l => l.ExerciseId);
        builder.Property(l => l.ClientResponseMs);
        builder.Property(l => l.ServerResponseMs);
        builder.Property(l => l.TimingAdjusted).HasDefaultValue(false).IsRequired();

        // One answer per exercise, also under a concurrent replay.
        builder.HasIndex(l => l.ExerciseId).IsUnique().HasFilter("[ExerciseId] IS NOT NULL");

        builder.HasIndex(l => new { l.UserId, l.OccurredAtUtc });
        builder.HasIndex(l => new { l.UserId, l.SenseId });

        // One row per attempt: a concurrent duplicate answer (double tap, client retry) gets the
        // same AttemptNo and fails here, so FSRS is never applied twice.
        builder.HasIndex(l => new { l.UserId, l.SessionId, l.SenseId, l.AttemptNo }).IsUnique();
    }
}
