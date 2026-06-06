using Microsoft.EntityFrameworkCore;
using WordBuddy.Domain.Entities;

namespace WordBuddy.Infrastructure.Persistence;

/// <summary>EF Core database context for the WordBuddy application.</summary>
public sealed class WordBuddyDbContext : DbContext
{
    /// <summary>Initializes a new <see cref="WordBuddyDbContext"/>.</summary>
    public WordBuddyDbContext(DbContextOptions<WordBuddyDbContext> options) : base(options) { }

    /// <summary>Gets the <see cref="User"/> table.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Gets the <see cref="Lesson"/> table.</summary>
    public DbSet<Lesson> Lessons => Set<Lesson>();

    /// <summary>Gets the <see cref="VocabularyItem"/> table.</summary>
    public DbSet<VocabularyItem> VocabularyItems => Set<VocabularyItem>();

    /// <summary>Gets the <see cref="GrammarRule"/> table.</summary>
    public DbSet<GrammarRule> GrammarRules => Set<GrammarRule>();

    /// <summary>Gets the <see cref="DailyPhrase"/> table.</summary>
    public DbSet<DailyPhrase> DailyPhrases => Set<DailyPhrase>();

    /// <summary>Gets the <see cref="MediaAsset"/> table.</summary>
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();

    /// <summary>Gets the <see cref="LearnerProgress"/> table.</summary>
    public DbSet<LearnerProgress> LearnerProgress => Set<LearnerProgress>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WordBuddyDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
