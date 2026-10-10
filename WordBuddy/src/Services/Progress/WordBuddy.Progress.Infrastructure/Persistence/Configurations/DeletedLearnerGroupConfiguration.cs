using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="DeletedLearnerGroup"/> to <c>DeletedLearnerGroups</c> (PK <c>GroupId</c>).</summary>
public sealed class DeletedLearnerGroupConfiguration : IEntityTypeConfiguration<DeletedLearnerGroup>
{
    public void Configure(EntityTypeBuilder<DeletedLearnerGroup> builder)
    {
        builder.ToTable("DeletedLearnerGroups");
        builder.HasKey(g => g.GroupId);
        builder.Property(g => g.GroupId).ValueGeneratedNever();
        builder.Property(g => g.DeletedAtUtc).IsRequired();
    }
}
