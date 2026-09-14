using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Infrastructure.Persistence.Configurations;

public sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("Lessons");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Title).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Description).HasMaxLength(2000).IsRequired();
        builder.Property(l => l.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(l => l.Level).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(l => l.TargetAgeGroup).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasMany(l => l.VocabularyItems)
            .WithOne()
            .HasForeignKey(v => v.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(l => l.VocabularyItems).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(l => l.GrammarRules)
            .WithOne()
            .HasForeignKey(g => g.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(l => l.GrammarRules).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(l => l.DailyPhrases)
            .WithOne()
            .HasForeignKey(d => d.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(l => l.DailyPhrases).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
