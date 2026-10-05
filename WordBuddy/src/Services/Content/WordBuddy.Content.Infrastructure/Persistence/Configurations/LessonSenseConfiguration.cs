using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="LessonSense"/> to <c>LessonSenses</c> (formerly <c>LessonVocabularyWords</c>).
/// The Lesson side (cascade on lesson delete) is configured in <see cref="LessonConfiguration"/>.</summary>
public sealed class LessonSenseConfiguration : IEntityTypeConfiguration<LessonSense>
{
    public void Configure(EntityTypeBuilder<LessonSense> builder)
    {
        builder.ToTable("LessonSenses");
        builder.HasKey(l => new { l.LessonId, l.SenseId });

        builder.Property(l => l.SortOrder).IsRequired();

        // NoAction: deleting a sense still linked to a lesson must fail loudly rather than silently
        // removing it from the lesson.
        builder.HasOne(l => l.Sense)
            .WithMany()
            .HasForeignKey(l => l.SenseId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
