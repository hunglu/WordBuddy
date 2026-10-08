using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="VocabularyLearnerSettings"/> to <c>VocabularyLearnerSettings</c> (PK <c>UserId</c>).</summary>
public sealed class VocabularyLearnerSettingsConfiguration : IEntityTypeConfiguration<VocabularyLearnerSettings>
{
    public void Configure(EntityTypeBuilder<VocabularyLearnerSettings> builder)
    {
        builder.ToTable("VocabularyLearnerSettings");
        builder.HasKey(s => s.UserId);
        builder.Property(s => s.UserId).ValueGeneratedNever();
        builder.Property(s => s.NewWordsPerDay);
        builder.Property(s => s.SupporterNewWordCap);
        builder.Property(s => s.SupporterCapSetBy);
    }
}
