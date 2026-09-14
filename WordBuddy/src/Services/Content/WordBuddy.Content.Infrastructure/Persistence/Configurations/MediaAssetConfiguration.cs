using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Infrastructure.Persistence.Configurations;

public sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable("MediaAssets");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(m => m.Url).HasMaxLength(1000).IsRequired();
    }
}
