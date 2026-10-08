using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="SupportLinkProjection"/> to <c>SupportLinkProjections</c> (PK <c>LinkId</c>).</summary>
public sealed class SupportLinkProjectionConfiguration : IEntityTypeConfiguration<SupportLinkProjection>
{
    public void Configure(EntityTypeBuilder<SupportLinkProjection> builder)
    {
        builder.ToTable("SupportLinkProjections");
        builder.HasKey(p => p.LinkId);
        builder.Property(p => p.LinkId).ValueGeneratedNever();
        builder.Property(p => p.LearnerId).IsRequired();
        builder.Property(p => p.SupporterId).IsRequired();
        builder.Property(p => p.IsActive).IsRequired();
        builder.Property(p => p.UpdatedAtUtc).IsRequired();

        builder.HasIndex(p => new { p.LearnerId, p.IsActive });
        builder.HasIndex(p => new { p.SupporterId, p.LearnerId });
    }
}
