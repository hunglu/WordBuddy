using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Progress.Domain.Challenges;

namespace WordBuddy.Progress.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="ChallengeProfile"/> to <c>ChallengeProfiles</c> (PK <c>UserId</c>). No email is stored.</summary>
public sealed class ChallengeProfileConfiguration : IEntityTypeConfiguration<ChallengeProfile>
{
    public void Configure(EntityTypeBuilder<ChallengeProfile> builder)
    {
        builder.ToTable("ChallengeProfiles");
        builder.HasKey(p => p.UserId);
        builder.Property(p => p.UserId).ValueGeneratedNever();

        builder.Property(p => p.AgeGroup).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(p => p.Alias).HasMaxLength(20);
        builder.Property(p => p.AvatarId).HasMaxLength(30);
        builder.Property(p => p.DisplayName).HasMaxLength(100);
        builder.Property(p => p.TimeZoneId).HasMaxLength(64);
        builder.Property(p => p.IsGuardian).IsRequired();
        builder.Property(p => p.UpdatedAtUtc).IsRequired();
        builder.Ignore(p => p.PublicName);
    }
}
