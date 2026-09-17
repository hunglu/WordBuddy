using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.Infrastructure.Persistence.Configurations;

public sealed class LearnerProgressConfiguration : IEntityTypeConfiguration<LearnerProgress>
{
    public void Configure(EntityTypeBuilder<LearnerProgress> builder)
    {
        builder.ToTable("LearnerProgressEntries");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.LessonId).IsRequired();
        builder.Property(p => p.IsCompleted).IsRequired();

        // One progress row per (user, lesson) — RecordProgress upserts against this.
        builder.HasIndex(p => new { p.UserId, p.LessonId }).IsUnique();
    }
}
