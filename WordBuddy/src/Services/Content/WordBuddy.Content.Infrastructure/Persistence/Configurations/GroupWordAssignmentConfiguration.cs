using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="GroupWordAssignment"/> to <c>GroupWordAssignments</c>.</summary>
public sealed class GroupWordAssignmentConfiguration : IEntityTypeConfiguration<GroupWordAssignment>
{
    public void Configure(EntityTypeBuilder<GroupWordAssignment> builder)
    {
        builder.ToTable("GroupWordAssignments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.GroupId).IsRequired();
        builder.Property(a => a.SenseId).IsRequired();
        builder.Property(a => a.AssignedBy).IsRequired();
        builder.Property(a => a.AssignedAtUtc).IsRequired();

        builder.HasIndex(a => a.GroupId);
    }
}
