using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="Sense"/> to <c>Senses</c> (formerly <c>VocabularyWords</c>, renamed in
/// place by <c>SplitVocabularyIntoLexemesAndSenses</c>).</summary>
public sealed class SenseConfiguration : IEntityTypeConfiguration<Sense>
{
    /// <summary>Filter for the learner-only uniqueness index. System (lesson) words are excluded on
    /// purpose: their ids name audio blobs, so identical system words may coexist.</summary>
    public const string LearnerOnlyFilter = "[Source] = 'Learner'";

    /// <summary>Name of the learner-only uniqueness index on (ContentHash, OwnerUserId).</summary>
    public const string ContentHashOwnerIndexName = "UX_Senses_ContentHash_OwnerUserId_Learner";

    public void Configure(EntityTypeBuilder<Sense> builder)
    {
        builder.ToTable("Senses");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Word).HasMaxLength(200).IsRequired();
        builder.Property(w => w.Definition).HasMaxLength(2000).IsRequired();
        builder.Property(w => w.Example).HasMaxLength(500);
        builder.Property(w => w.LexemeId).IsRequired();
        builder.Property(w => w.ContentHash).HasMaxLength(64).IsUnicode(false).IsFixedLength().IsRequired();
        builder.Property(w => w.Source).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(w => w.OwnerUserId).IsRequired();
        builder.Property(w => w.OwnerAgeGroup).HasConversion<string>().HasMaxLength(20);
        builder.Property(w => w.ShareStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(w => w.VisibleToChildren).IsRequired();
        builder.Property(w => w.CreatedAtUtc).IsRequired();
        builder.Property(w => w.ModeratedAtUtc);
        builder.Property(w => w.ModeratedByUserId);

        builder.HasIndex(w => w.ContentHash);
        builder.HasIndex(w => new { w.ContentHash, w.OwnerUserId })
            .IsUnique()
            .HasFilter(LearnerOnlyFilter)
            .HasDatabaseName(ContentHashOwnerIndexName);
        builder.HasIndex(w => w.OwnerUserId);
        builder.HasIndex(w => w.ShareStatus);

        // NoAction: a Lexeme is only removed by the guarded orphan delete in the repository, never
        // by a cascade.
        builder.HasOne<Lexeme>()
            .WithMany()
            .HasForeignKey(w => w.LexemeId)
            .OnDelete(DeleteBehavior.NoAction);

        // NoAction, not SetNull — SQL Server rejects a second cascade-ish path (SetNull) into
        // MediaAssets alongside the cascades into the link tables ("multiple cascade paths").
        // MediaAsset rows are reference-only; leaving one orphaned is harmless.
        builder.HasOne(w => w.Audio)
            .WithMany()
            .HasForeignKey(w => w.AudioAssetId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
