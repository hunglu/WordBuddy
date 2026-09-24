using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.Infrastructure.Persistence.Configurations;

public sealed class VocabularyRecallSessionConfiguration : IEntityTypeConfiguration<VocabularyRecallSession>
{
    public void Configure(EntityTypeBuilder<VocabularyRecallSession> builder)
    {
        builder.ToTable("VocabularyRecallSessions");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId).IsRequired();
        builder.Property(s => s.CheckedAtUtc).IsRequired();
        builder.Property(s => s.WordsChecked).IsRequired();
        builder.Property(s => s.WordsKnown).IsRequired();

        builder.HasIndex(s => new { s.UserId, s.CheckedAtUtc });
    }
}
