using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="LearnerGroupMemberProjection"/> to <c>LearnerGroupMemberProjections</c> (PK <c>GroupId</c> + <c>LearnerId</c>).</summary>
public sealed class LearnerGroupMemberProjectionConfiguration : IEntityTypeConfiguration<LearnerGroupMemberProjection>
{
    public void Configure(EntityTypeBuilder<LearnerGroupMemberProjection> builder)
    {
        builder.ToTable("LearnerGroupMemberProjections");
        builder.HasKey(p => new { p.GroupId, p.LearnerId });
        builder.Property(p => p.GroupId).ValueGeneratedNever();
        builder.Property(p => p.LearnerId).ValueGeneratedNever();
        builder.Property(p => p.OwnerId).IsRequired();
        builder.Property(p => p.IsActive).IsRequired();
        builder.Property(p => p.UpdatedAtUtc).IsRequired();
    }
}
