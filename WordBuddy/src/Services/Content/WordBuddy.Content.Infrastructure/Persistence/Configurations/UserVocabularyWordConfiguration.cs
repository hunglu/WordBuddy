using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Infrastructure.Persistence.Configurations;

public sealed class UserVocabularyWordConfiguration : IEntityTypeConfiguration<UserVocabularyWord>
{
    public void Configure(EntityTypeBuilder<UserVocabularyWord> builder)
    {
        builder.ToTable("UserVocabularyWords");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.UserId).IsRequired();
        builder.Property(l => l.VocabularyWordId).IsRequired();
        builder.Property(l => l.AddedAtUtc).IsRequired();
        builder.Property(l => l.IsAuthor).IsRequired();

        builder.HasIndex(l => new { l.UserId, l.VocabularyWordId }).IsUnique();

        builder.HasOne(l => l.VocabularyWord)
            .WithMany()
            .HasForeignKey(l => l.VocabularyWordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
