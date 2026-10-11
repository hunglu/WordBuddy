using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Progress.Domain.Challenges;

namespace WordBuddy.Progress.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="ChallengeConsent"/> to <c>ChallengeConsents</c> (PK <c>ChildId + EnabledBy</c>).</summary>
public sealed class ChallengeConsentConfiguration : IEntityTypeConfiguration<ChallengeConsent>
{
    public void Configure(EntityTypeBuilder<ChallengeConsent> builder)
    {
        builder.ToTable("ChallengeConsents");
        builder.HasKey(c => new { c.ChildId, c.EnabledBy });
        builder.Property(c => c.ChildId).ValueGeneratedNever();
        builder.Property(c => c.EnabledBy).ValueGeneratedNever();
        builder.Property(c => c.EnabledAtUtc).IsRequired();

        builder.HasIndex(c => c.EnabledBy);
    }
}
