using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WordBuddy.Domain.Entities;

namespace WordBuddy.Infrastructure.Persistence.Configurations;

internal sealed class GrammarRuleConfiguration : IEntityTypeConfiguration<GrammarRule>
{
    public void Configure(EntityTypeBuilder<GrammarRule> builder)
    {
        builder.ToTable("GrammarRules");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RuleName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Explanation)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.OrderIndex).IsRequired();

        // Stored as a JSON array; EF Core constructor binding populates the read-only property.
        ValueConverter<IReadOnlyList<string>, string> examplesConverter = new(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => (IReadOnlyList<string>)(JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)
                 ?? new List<string>()));

        builder.Property(x => x.Examples)
            .HasConversion(examplesConverter)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.HasOne<Lesson>()
            .WithMany()
            .HasForeignKey(x => x.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
