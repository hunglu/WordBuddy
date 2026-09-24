using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.Infrastructure.Persistence.Configurations;

public sealed class VocabularyRecallStatConfiguration : IEntityTypeConfiguration<VocabularyRecallStat>
{
    public void Configure(EntityTypeBuilder<VocabularyRecallStat> builder)
    {
        builder.ToTable("VocabularyRecallStats");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId).IsRequired();
        builder.Property(s => s.VocabularyWordId).IsRequired();
        builder.Property(s => s.Word).HasMaxLength(200).IsRequired();
        builder.Property(s => s.TimesChecked).IsRequired();
        builder.Property(s => s.TimesKnown).IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.LastCheckedAtUtc).IsRequired();

        // One stat row per (user, word) — SubmitVocabularyRecallCheck upserts against this.
        builder.HasIndex(s => new { s.UserId, s.VocabularyWordId }).IsUnique();
    }
}
