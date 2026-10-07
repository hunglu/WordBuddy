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

        builder.HasIndex(l => new { l.UserId, l.OccurredAtUtc });
        builder.HasIndex(l => new { l.UserId, l.SenseId });
    }
}
