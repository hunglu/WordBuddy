using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Identity.Domain.SupportLinks;

namespace WordBuddy.Identity.Infrastructure.Persistence.Configurations;

public sealed class SupportLinkAuditEntryConfiguration : IEntityTypeConfiguration<SupportLinkAuditEntry>
{
    public void Configure(EntityTypeBuilder<SupportLinkAuditEntry> builder)
    {
        builder.ToTable("SupportLinkAuditEntries");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.LinkId).IsRequired();
        builder.Property(a => a.ActorId).IsRequired();
        builder.Property(a => a.Action).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(a => a.AtUtc).IsRequired();
        builder.Property(a => a.Reason).HasMaxLength(SupportLinkAuditEntry.ReasonMaxLength);

        builder.HasIndex(a => a.LinkId);
    }
}
