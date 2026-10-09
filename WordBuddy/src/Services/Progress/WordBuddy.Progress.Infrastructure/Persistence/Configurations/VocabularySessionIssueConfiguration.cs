using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="VocabularySessionIssue"/> to <c>VocabularySessionIssues</c> (PK <c>SessionId</c>) and its items to <c>VocabularySessionIssueItems</c>.</summary>
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
        builder.Property(i => i.DurationMinutes).IsRequired();
        builder.Property(i => i.ExpiresAtUtc).IsRequired();
        builder.Property(i => i.EndedAtUtc);

        builder.HasMany(i => i.Items)
            .WithOne()
            .HasForeignKey(i => i.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(i => i.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(i => new { i.UserId, i.IssuedAtUtc });
    }
}

/// <summary>Maps <see cref="VocabularySessionIssueItem"/> (PK <c>SessionId</c> + <c>SenseId</c>).</summary>
public sealed class VocabularySessionIssueItemConfiguration : IEntityTypeConfiguration<VocabularySessionIssueItem>
{
    public void Configure(EntityTypeBuilder<VocabularySessionIssueItem> builder)
    {
        builder.ToTable("VocabularySessionIssueItems");
        builder.HasKey(i => new { i.SessionId, i.SenseId });
        builder.Property(i => i.IsNew).IsRequired();
        builder.Property(i => i.Position).IsRequired();
    }
}
