using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Identity.Domain.SupportLinks;

namespace WordBuddy.Identity.Infrastructure.Persistence.Configurations;

public sealed class SupportLinkConfiguration : IEntityTypeConfiguration<SupportLink>
{
    public void Configure(EntityTypeBuilder<SupportLink> builder)
    {
        builder.ToTable("SupportLinks");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.LearnerId).IsRequired();
        builder.Property(l => l.SupporterId).IsRequired();
        builder.Property(l => l.IsPrimary).IsRequired();
        builder.Property(l => l.Relationship).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(l => l.GrantedBy).IsRequired();
        builder.Property(l => l.CreatedAtUtc).IsRequired();
        builder.Property(l => l.UpdatedAtUtc).IsRequired();
        builder.Ignore(l => l.IsActive);

        builder.HasIndex(l => l.LearnerId);
        builder.HasIndex(l => l.SupporterId);

        // One active Primary per learner.
        builder.HasIndex(l => l.LearnerId, "IX_SupportLinks_LearnerId_ActivePrimary")
            .IsUnique()
            .HasFilter("[IsPrimary] = 1 AND [Status] = 'Active'");
    }
}
