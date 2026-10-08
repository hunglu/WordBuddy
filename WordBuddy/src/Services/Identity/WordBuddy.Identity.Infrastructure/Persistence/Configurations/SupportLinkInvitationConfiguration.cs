using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Identity.Domain.SupportLinks;

namespace WordBuddy.Identity.Infrastructure.Persistence.Configurations;

public sealed class SupportLinkInvitationConfiguration : IEntityTypeConfiguration<SupportLinkInvitation>
{
    public void Configure(EntityTypeBuilder<SupportLinkInvitation> builder)
    {
        builder.ToTable("SupportLinkInvitations");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.CreatedById).IsRequired();
        builder.Property(i => i.CreatorSide).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(i => i.Relationship).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.CodeHash).HasMaxLength(64).IsRequired();
        builder.Property(i => i.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(i => i.CreatedAtUtc).IsRequired();
        builder.Property(i => i.ExpiresAtUtc).IsRequired();

        builder.HasIndex(i => i.CodeHash).IsUnique();
        builder.HasIndex(i => i.TokenHash).IsUnique();
        builder.HasIndex(i => i.CreatedById);
    }
}
