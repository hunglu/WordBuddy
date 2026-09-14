using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Quiz.Domain;

namespace WordBuddy.Quiz.Infrastructure.Persistence.Configurations;

public sealed class QuizConfiguration : IEntityTypeConfiguration<Domain.Quiz>
{
    public void Configure(EntityTypeBuilder<Domain.Quiz> builder)
    {
        builder.ToTable("Quizzes");
        builder.HasKey(q => q.Id);

        builder.Property(q => q.Title).HasMaxLength(200).IsRequired();
        builder.Property(q => q.Description).HasMaxLength(2000).IsRequired();
        builder.Property(q => q.Level).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(q => q.TargetAgeGroup).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(q => q.LessonId).IsRequired();

        builder.HasMany(q => q.Questions)
            .WithOne()
            .HasForeignKey(q => q.QuizId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(q => q.Questions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
