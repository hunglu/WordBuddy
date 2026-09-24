using Microsoft.EntityFrameworkCore;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Infrastructure.Persistence;

/// <summary>EF Core context for the Content service's own database (<c>WordBuddyContent</c>).</summary>
public sealed class ContentDbContext : DbContext
{
    public ContentDbContext(DbContextOptions<ContentDbContext> options) : base(options)
    {
    }

    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<VocabularyItem> VocabularyItems => Set<VocabularyItem>();
    public DbSet<GrammarRule> GrammarRules => Set<GrammarRule>();
    public DbSet<DailyPhrase> DailyPhrases => Set<DailyPhrase>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<PersonalVocabularyWord> PersonalVocabularyWords => Set<PersonalVocabularyWord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContentDbContext).Assembly);
    }
}
