using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.Infrastructure.Persistence.Configurations;

/// <summary>Maps the insert-only <see cref="VocabularySessionIssue"/> to <c>VocabularySessionIssues</c> (PK <c>SessionId</c>).</summary>
public sealed class VocabularySessionIssueConfiguration : IEntityTypeConfiguration<VocabularySessionIssue>
{
    public void Configure(EntityTypeBuilder<VocabularySessionIssue> builder)
    {
        builder.ToTable("VocabularySessionIssues");
        builder.HasKey(i => i.SessionId);
        builder.Property(i => i.SessionId).ValueGeneratedNever();
        builder.Property(i => i.UserId).IsRequired();
        builder.Property(i => i.IssuedAtUtc).IsRequired();
        builder.Property(i => i.PlannedCount).IsRequired();

        builder.HasIndex(i => new { i.UserId, i.IssuedAtUtc });
    }
}
