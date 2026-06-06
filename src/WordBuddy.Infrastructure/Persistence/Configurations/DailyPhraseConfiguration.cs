using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Domain.Entities;

namespace WordBuddy.Infrastructure.Persistence.Configurations;

internal sealed class DailyPhraseConfiguration : IEntityTypeConfiguration<DailyPhrase>
{
    public void Configure(EntityTypeBuilder<DailyPhrase> builder)
    {
        builder.ToTable("DailyPhrases");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Phrase)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Meaning)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.UsageContext)
            .IsRequired()
            .HasMaxLength(1000);

        builder.HasOne<Lesson>()
            .WithMany()
            .HasForeignKey(x => x.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
