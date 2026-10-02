using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WordBuddy.Content.Infrastructure.Persistence.Configurations;

public sealed class VocabularyWordIdRemapConfiguration : IEntityTypeConfiguration<VocabularyWordIdRemap>
{
    public void Configure(EntityTypeBuilder<VocabularyWordIdRemap> builder)
    {
        builder.ToTable("VocabularyWordIdRemaps");
        builder.HasKey(r => r.OldId);

        builder.Property(r => r.NewId).IsRequired();
        builder.Property(r => r.PublishedAtUtc);

        builder.HasIndex(r => r.PublishedAtUtc);
    }
}
