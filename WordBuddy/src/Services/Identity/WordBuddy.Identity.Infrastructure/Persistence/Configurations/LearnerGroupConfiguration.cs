using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Identity.Domain.Groups;

namespace WordBuddy.Identity.Infrastructure.Persistence.Configurations;

public sealed class LearnerGroupConfiguration : IEntityTypeConfiguration<LearnerGroup>
{
    public void Configure(EntityTypeBuilder<LearnerGroup> builder)
    {
        builder.ToTable("LearnerGroups");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();

        builder.Property(g => g.OwnerId).IsRequired();
        builder.Property(g => g.Name).HasMaxLength(LearnerGroup.NameMaxLength).IsRequired();
        builder.Property(g => g.CreatedAtUtc).IsRequired();
        builder.Property(g => g.UpdatedAtUtc).IsRequired();
        builder.Property(g => g.DeletedAtUtc);
        builder.Ignore(g => g.IsDeleted);

        builder.HasIndex(g => g.OwnerId);
    }
}
