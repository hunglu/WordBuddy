using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Identity.Domain.Groups;

namespace WordBuddy.Identity.Infrastructure.Persistence.Configurations;

public sealed class LearnerGroupMemberConfiguration : IEntityTypeConfiguration<LearnerGroupMember>
{
    public void Configure(EntityTypeBuilder<LearnerGroupMember> builder)
    {
        builder.ToTable("LearnerGroupMembers");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.GroupId).IsRequired();
        builder.Property(m => m.LearnerId).IsRequired();
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(m => m.AddedAtUtc).IsRequired();
        builder.Property(m => m.UpdatedAtUtc).IsRequired();
        builder.Property(m => m.RemovedReason).HasConversion<string>().HasMaxLength(30);
        builder.Ignore(m => m.IsActive);

        builder.HasOne<LearnerGroup>().WithMany().HasForeignKey(m => m.GroupId).OnDelete(DeleteBehavior.Restrict);

        // A learner is pending or active at most once per group.
        builder.HasIndex(m => new { m.GroupId, m.LearnerId }, "IX_LearnerGroupMembers_GroupId_LearnerId_Open")
            .IsUnique()
            .HasFilter("[Status] <> 'Removed'");
        builder.HasIndex(m => m.LearnerId);
    }
}
