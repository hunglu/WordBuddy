using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="LearnerWord"/> to <c>LearnerWords</c> (formerly <c>UserVocabularyWords</c>).</summary>
public sealed class LearnerWordConfiguration : IEntityTypeConfiguration<LearnerWord>
{
    public void Configure(EntityTypeBuilder<LearnerWord> builder)
    {
        builder.ToTable("LearnerWords");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.UserId).IsRequired();
        builder.Property(l => l.SenseId).IsRequired();
        builder.Property(l => l.AddedAtUtc).IsRequired();
        builder.Property(l => l.IsAuthor).IsRequired();
        builder.Property(l => l.AddedBy)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(LearnerWordAddedBy.Learner)
            .IsRequired();
        builder.Property(l => l.PersonalContext).HasMaxLength(LearnerWord.PersonalContextMaxLength);

        builder.HasIndex(l => new { l.UserId, l.SenseId }).IsUnique();

        builder.HasOne(l => l.Sense)
            .WithMany()
            .HasForeignKey(l => l.SenseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
