using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="SenseTranslation"/> to <c>SenseTranslations</c>.</summary>
public sealed class SenseTranslationConfiguration : IEntityTypeConfiguration<SenseTranslation>
{
    public void Configure(EntityTypeBuilder<SenseTranslation> builder)
    {
        builder.ToTable("SenseTranslations");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Locale).HasMaxLength(SenseTranslation.LocaleMaxLength).IsRequired();
        builder.Property(t => t.Text).HasMaxLength(SenseTranslation.TextMaxLength).IsRequired();

        builder.HasIndex(t => new { t.SenseId, t.Locale })
            .IsUnique()
            .HasDatabaseName("UX_SenseTranslations_SenseId_Locale");

        builder.HasOne<Sense>()
            .WithMany()
            .HasForeignKey(t => t.SenseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
