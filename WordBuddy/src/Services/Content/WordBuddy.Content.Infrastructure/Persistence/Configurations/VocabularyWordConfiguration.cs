using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Infrastructure.Persistence.Configurations;

public sealed class VocabularyWordConfiguration : IEntityTypeConfiguration<VocabularyWord>
{
    /// <summary>Filter for the learner-only uniqueness index. System (lesson) words are excluded on
    /// purpose: their ids name audio blobs, so identical system words may coexist.</summary>
    public const string LearnerOnlyFilter = "[Source] = 'Learner'";

    public void Configure(EntityTypeBuilder<VocabularyWord> builder)
    {
        builder.ToTable("VocabularyWords");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Word).HasMaxLength(200).IsRequired();
        builder.Property(w => w.Definition).HasMaxLength(2000).IsRequired();
        builder.Property(w => w.Example).HasMaxLength(500);
        builder.Property(w => w.NormalizedWord).HasMaxLength(200).IsRequired();
        builder.Property(w => w.ContentHash).HasMaxLength(64).IsUnicode(false).IsFixedLength().IsRequired();
        builder.Property(w => w.Source).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(w => w.OwnerUserId).IsRequired();
        builder.Property(w => w.OwnerAgeGroup).HasConversion<string>().HasMaxLength(20);
        builder.Property(w => w.ShareStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(w => w.VisibleToChildren).IsRequired();
        builder.Property(w => w.CreatedAtUtc).IsRequired();
        builder.Property(w => w.ModeratedAtUtc);
        builder.Property(w => w.ModeratedByUserId);

        builder.HasIndex(w => w.NormalizedWord);
        builder.HasIndex(w => w.ContentHash);
        builder.HasIndex(w => new { w.ContentHash, w.OwnerUserId })
            .IsUnique()
            .HasFilter(LearnerOnlyFilter)
            .HasDatabaseName("UX_VocabularyWords_ContentHash_OwnerUserId_Learner");
        builder.HasIndex(w => w.OwnerUserId);
        builder.HasIndex(w => w.ShareStatus);

        // NoAction, not SetNull — SQL Server rejects a second cascade-ish path (SetNull) into
        // MediaAssets alongside the cascades into the link tables ("multiple cascade paths").
        // MediaAsset rows are reference-only; leaving one orphaned is harmless.
        builder.HasOne(w => w.Audio)
            .WithMany()
            .HasForeignKey(w => w.AudioAssetId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
