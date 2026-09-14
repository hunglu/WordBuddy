using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Infrastructure.Persistence.Configurations;

public sealed class GrammarRuleConfiguration : IEntityTypeConfiguration<GrammarRule>
{
    public void Configure(EntityTypeBuilder<GrammarRule> builder)
    {
        builder.ToTable("GrammarRules");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Title).HasMaxLength(200).IsRequired();
        builder.Property(g => g.Explanation).HasMaxLength(2000).IsRequired();

        // Examples as a JSON column (per root CLAUDE.md conventions), stored as an
        // IReadOnlyList<string> — round-tripped through a plain string[] for serialization.
        builder.Property(g => g.Examples)
            .HasConversion(
                examples => JsonSerializer.Serialize(examples, JsonSerializerOptions.Default),
                json => JsonSerializer.Deserialize<string[]>(json, JsonSerializerOptions.Default) ?? Array.Empty<string>(),
                new ValueComparer<IReadOnlyList<string>>(
                    (a, b) => (a ?? Array.Empty<string>()).SequenceEqual(b ?? Array.Empty<string>()),
                    list => list.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                    list => list.ToList()))
            .HasColumnType("nvarchar(max)");
    }
}
