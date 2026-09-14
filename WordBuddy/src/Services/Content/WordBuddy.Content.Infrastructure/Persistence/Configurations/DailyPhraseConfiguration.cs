using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Infrastructure.Persistence.Configurations;

public sealed class DailyPhraseConfiguration : IEntityTypeConfiguration<DailyPhrase>
{
    public void Configure(EntityTypeBuilder<DailyPhrase> builder)
    {
        builder.ToTable("DailyPhrases");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Phrase).HasMaxLength(500).IsRequired();
        builder.Property(d => d.Translation).HasMaxLength(500).IsRequired();

        // NoAction, not SetNull — see the comment in VocabularyItemConfiguration for why.
        builder.HasOne(d => d.Audio)
            .WithMany()
            .HasForeignKey(d => d.AudioAssetId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(d => d.Video)
            .WithMany()
            .HasForeignKey(d => d.VideoAssetId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
