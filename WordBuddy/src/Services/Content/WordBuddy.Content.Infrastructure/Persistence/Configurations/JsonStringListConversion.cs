using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WordBuddy.Content.Infrastructure.Persistence.Configurations;

/// <summary>Maps an <see cref="IReadOnlyList{T}"/> of strings to an <c>nvarchar(max)</c> JSON
/// column with default <c>[]</c> — the same shape as <c>Lexemes.WordForms</c>.</summary>
internal static class JsonStringListConversion
{
    /// <summary>Applies the converter, comparer, column type and default.</summary>
    public static PropertyBuilder<IReadOnlyList<string>> AsJsonStringList(this PropertyBuilder<IReadOnlyList<string>> property) =>
        property
            .HasConversion(
                items => JsonSerializer.Serialize(items, JsonSerializerOptions.Default),
                json => JsonSerializer.Deserialize<string[]>(json, JsonSerializerOptions.Default) ?? Array.Empty<string>(),
                new ValueComparer<IReadOnlyList<string>>(
                    (a, b) => (a ?? Array.Empty<string>()).SequenceEqual(b ?? Array.Empty<string>()),
                    list => list.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                    list => list.ToList()))
            .HasColumnType("nvarchar(max)")
            .HasDefaultValueSql("N'[]'")
            .IsRequired();
}
