using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Infrastructure.Persistence.Configurations;

public sealed class VocabularyItemConfiguration : IEntityTypeConfiguration<VocabularyItem>
{
    public void Configure(EntityTypeBuilder<VocabularyItem> builder)
    {
        builder.ToTable("VocabularyItems");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Word).HasMaxLength(200).IsRequired();
        builder.Property(v => v.Definition).HasMaxLength(2000).IsRequired();
        builder.Property(v => v.Example).HasMaxLength(500).IsRequired();

        // NoAction, not SetNull — SQL Server rejects the combination of Lesson's cascade delete
        // into VocabularyItems alongside a second cascade-ish path (SetNull) into MediaAssets
        // ("multiple cascade paths"). MediaAsset rows are reference-only anyway; leaving one
        // orphaned after a lesson delete is harmless.
        builder.HasOne(v => v.Audio)
            .WithMany()
            .HasForeignKey(v => v.AudioAssetId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
