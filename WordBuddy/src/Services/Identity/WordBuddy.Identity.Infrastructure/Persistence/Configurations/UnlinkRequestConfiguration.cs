using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Identity.Domain.SupportLinks;

namespace WordBuddy.Identity.Infrastructure.Persistence.Configurations;

public sealed class UnlinkRequestConfiguration : IEntityTypeConfiguration<UnlinkRequest>
{
    public void Configure(EntityTypeBuilder<UnlinkRequest> builder)
    {
        builder.ToTable("UnlinkRequests");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.LinkId).IsRequired();
        builder.Property(r => r.RequestedById).IsRequired();
        builder.Property(r => r.RequestedBySide).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.RequestedAtUtc).IsRequired();
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Ignore(r => r.IsOpen);

        builder.HasOne<SupportLink>().WithMany().HasForeignKey(r => r.LinkId).OnDelete(DeleteBehavior.Restrict);

        // One open request per link.
        builder.HasIndex(r => r.LinkId, "IX_UnlinkRequests_LinkId_Open")
            .IsUnique()
            .HasFilter("[Status] IN ('Pending', 'OverrideRequested')");
        builder.HasIndex(r => r.Status);
    }
}
