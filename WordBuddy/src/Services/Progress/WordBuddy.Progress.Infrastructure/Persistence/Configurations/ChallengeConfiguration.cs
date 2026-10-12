using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Progress.Domain.Challenges;

namespace WordBuddy.Progress.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="Challenge"/> to <c>Challenges</c>.</summary>
public sealed class ChallengeConfiguration : IEntityTypeConfiguration<Challenge>
{
    public void Configure(EntityTypeBuilder<Challenge> builder)
    {
        builder.ToTable("Challenges");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.TemplateKey).HasMaxLength(40).IsRequired();
        builder.Property(c => c.Audience).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(c => c.Kind).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(c => c.Metric).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(c => c.Target);
        builder.Property(c => c.OpensAtUtc).IsRequired();
        builder.Property(c => c.StartsAtUtc).IsRequired();
        builder.Property(c => c.EndsAtUtc).IsRequired();
        builder.Property(c => c.Capacity).IsRequired();
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(10).IsRequired();

        builder.HasIndex(c => new { c.Audience, c.Status });
        builder.HasIndex(c => new { c.TemplateKey, c.Audience, c.StartsAtUtc });
    }
}
