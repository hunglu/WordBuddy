using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Infrastructure.Persistence.Configurations;

/// <summary>The Lesson side (cascade on lesson delete) is configured in <see cref="LessonConfiguration"/>.</summary>
public sealed class LessonVocabularyWordConfiguration : IEntityTypeConfiguration<LessonVocabularyWord>
{
    public void Configure(EntityTypeBuilder<LessonVocabularyWord> builder)
    {
        builder.ToTable("LessonVocabularyWords");
        builder.HasKey(l => new { l.LessonId, l.VocabularyWordId });

        builder.Property(l => l.SortOrder).IsRequired();

        // NoAction: deleting a word still linked to a lesson must fail loudly rather than silently
        // removing it from the lesson.
        builder.HasOne(l => l.VocabularyWord)
            .WithMany()
            .HasForeignKey(l => l.VocabularyWordId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
