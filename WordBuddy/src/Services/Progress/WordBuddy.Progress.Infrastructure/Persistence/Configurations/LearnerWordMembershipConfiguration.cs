using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="LearnerWordMembership"/> to <c>LearnerWordMemberships</c>.</summary>
public sealed class LearnerWordMembershipConfiguration : IEntityTypeConfiguration<LearnerWordMembership>
{
    public void Configure(EntityTypeBuilder<LearnerWordMembership> builder)
    {
        builder.ToTable("LearnerWordMemberships");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.UserId).IsRequired();
        builder.Property(m => m.SenseId).IsRequired();
        builder.Property(m => m.AddedBy).IsRequired();
        builder.Property(m => m.AddedAtUtc).IsRequired();
        builder.Property(m => m.IsActive).IsRequired();
        builder.Property(m => m.LastEventAtUtc).IsRequired();

        // One row per (user, sense) — the consumers upsert against this (natural-key dedupe).
        builder.HasIndex(m => new { m.UserId, m.SenseId }).IsUnique();
    }
}
