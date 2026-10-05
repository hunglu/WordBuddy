using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="Lexeme"/> to <c>Lexemes</c>.</summary>
public sealed class LexemeConfiguration : IEntityTypeConfiguration<Lexeme>
{
    /// <summary>Name of the unique (NormalizedLemma, PartOfSpeech) index.</summary>
    public const string LemmaPartOfSpeechIndexName = "UX_Lexemes_NormalizedLemma_PartOfSpeech";

    public void Configure(EntityTypeBuilder<Lexeme> builder)
    {
        builder.ToTable("Lexemes");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Lemma).HasMaxLength(Lexeme.LemmaMaxLength).IsRequired();
        builder.Property(l => l.NormalizedLemma).HasMaxLength(Lexeme.LemmaMaxLength).IsRequired();
        builder.Property(l => l.PartOfSpeech).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.IpaUk).HasMaxLength(100);
        builder.Property(l => l.IpaUs).HasMaxLength(100);
        builder.Property(l => l.Syllables).HasMaxLength(200);
        builder.Property(l => l.CefrLevel).HasConversion<string>().HasMaxLength(2);
        builder.Property(l => l.FrequencyRank);
        builder.Property(l => l.CreatedAtUtc).IsRequired();

        // Same JSON converter as GrammarRules.Examples.
        builder.Property(l => l.WordForms)
            .HasConversion(
                forms => JsonSerializer.Serialize(forms, JsonSerializerOptions.Default),
                json => JsonSerializer.Deserialize<string[]>(json, JsonSerializerOptions.Default) ?? Array.Empty<string>(),
                new ValueComparer<IReadOnlyList<string>>(
                    (a, b) => (a ?? Array.Empty<string>()).SequenceEqual(b ?? Array.Empty<string>()),
                    list => list.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                    list => list.ToList()))
            .HasColumnType("nvarchar(max)")
            .HasDefaultValueSql("N'[]'")
            .IsRequired();

        // HasFilter(null) is essential: EF would otherwise add "WHERE [PartOfSpeech] IS NOT NULL",
        // which allows duplicate (lemma, NULL) rows and breaks lexeme dedupe.
        builder.HasIndex(l => new { l.NormalizedLemma, l.PartOfSpeech })
            .IsUnique()
            .HasFilter(null)
            .HasDatabaseName(LemmaPartOfSpeechIndexName);

        builder.HasOne(l => l.UkAudio)
            .WithMany()
            .HasForeignKey(l => l.UkAudioAssetId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(l => l.UsAudio)
            .WithMany()
            .HasForeignKey(l => l.UsAudioAssetId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
