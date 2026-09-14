using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Quiz.Domain;

namespace WordBuddy.Quiz.Infrastructure.Persistence.Configurations;

public sealed class QuizQuestionConfiguration : IEntityTypeConfiguration<QuizQuestion>
{
    public void Configure(EntityTypeBuilder<QuizQuestion> builder)
    {
        builder.ToTable("QuizQuestions");
        builder.HasKey(q => q.Id);

        builder.Property(q => q.Text).HasMaxLength(500).IsRequired();
        builder.Property(q => q.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(q => q.Explanation).HasMaxLength(1000).IsRequired();
        builder.Property(q => q.CorrectOptionIndex).IsRequired();

        // Options as a JSON column, same pattern as Content's GrammarRule.Examples.
        builder.Property(q => q.Options)
            .HasConversion(
                options => JsonSerializer.Serialize(options, JsonSerializerOptions.Default),
                json => JsonSerializer.Deserialize<string[]>(json, JsonSerializerOptions.Default) ?? Array.Empty<string>(),
                new ValueComparer<IReadOnlyList<string>>(
                    (a, b) => (a ?? Array.Empty<string>()).SequenceEqual(b ?? Array.Empty<string>()),
                    list => list.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                    list => list.ToList()))
            .HasColumnType("nvarchar(max)");
    }
}
