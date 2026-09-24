using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Infrastructure.Persistence.Configurations;

public sealed class PersonalVocabularyWordConfiguration : IEntityTypeConfiguration<PersonalVocabularyWord>
{
    public void Configure(EntityTypeBuilder<PersonalVocabularyWord> builder)
    {
        builder.ToTable("PersonalVocabularyWords");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.OwnerUserId).IsRequired();
        builder.Property(w => w.OwnerAgeGroup).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(w => w.Word).HasMaxLength(200).IsRequired();
        builder.Property(w => w.Definition).HasMaxLength(2000).IsRequired();
        builder.Property(w => w.Example).HasMaxLength(500);
        builder.Property(w => w.ShareStatus).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(w => w.VisibleToChildren).IsRequired();
        builder.Property(w => w.CreatedAtUtc).IsRequired();

        builder.HasIndex(w => w.OwnerUserId);
        builder.HasIndex(w => w.ShareStatus);
    }
}
