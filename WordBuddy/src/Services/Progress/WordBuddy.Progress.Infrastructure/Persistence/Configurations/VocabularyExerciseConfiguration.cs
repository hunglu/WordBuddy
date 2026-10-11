using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="VocabularyExercise"/> to <c>VocabularyExercises</c>. Options are stored as JSON.</summary>
public sealed class VocabularyExerciseConfiguration : IEntityTypeConfiguration<VocabularyExercise>
{
    public void Configure(EntityTypeBuilder<VocabularyExercise> builder)
    {
        builder.ToTable("VocabularyExercises");
        builder.HasKey(e => e.Id);
        builder.Ignore(e => e.ExerciseId);

        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.SessionId).IsRequired();
        builder.Property(e => e.SenseId).IsRequired();
        builder.Property(e => e.ExerciseType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(e => e.Skill).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(e => e.ExpectedAnswer).HasMaxLength(200).IsRequired();
        builder.Property(e => e.CorrectWord).HasMaxLength(200).IsRequired();
        builder.Property(e => e.IssuedAtUtc).IsRequired();
        builder.Property(e => e.AnsweredAtUtc);

        builder.Property(e => e.Options)
            .HasConversion(
                options => JsonSerializer.Serialize(options, (JsonSerializerOptions?)null),
                json => (IReadOnlyDictionary<string, Guid>)(JsonSerializer.Deserialize<Dictionary<string, Guid>>(json, (JsonSerializerOptions?)null)
                    ?? new Dictionary<string, Guid>()),
                new ValueComparer<IReadOnlyDictionary<string, Guid>>(
                    (a, b) => ReferenceEquals(a, b) || (a != null && b != null && a.Count == b.Count && !a.Except(b).Any()),
                    d => d.Aggregate(0, (hash, pair) => HashCode.Combine(hash, pair.Key, pair.Value)),
                    d => d.ToDictionary(pair => pair.Key, pair => pair.Value)))
            .HasMaxLength(1000)
            .IsRequired();

        builder.HasIndex(e => e.SessionId);
        builder.HasIndex(e => e.UserId);
    }
}
