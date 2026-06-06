using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Domain.Entities;

namespace WordBuddy.Infrastructure.Persistence.Configurations;

internal sealed class LearnerProgressConfiguration : IEntityTypeConfiguration<LearnerProgress>
{
    public void Configure(EntityTypeBuilder<LearnerProgress> builder)
    {
        builder.ToTable("LearnerProgress");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.UserId, x.LessonId })
            .IsUnique();

        builder.Property(x => x.IsCompleted).IsRequired();
        builder.Property(x => x.LastAccessedAt).IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Lesson>()
            .WithMany()
            .HasForeignKey(x => x.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
