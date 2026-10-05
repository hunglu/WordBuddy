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
    public DbSet<GrammarRule> GrammarRules => Set<GrammarRule>();
    public DbSet<DailyPhrase> DailyPhrases => Set<DailyPhrase>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<Lexeme> Lexemes => Set<Lexeme>();
    public DbSet<Sense> Senses => Set<Sense>();
    public DbSet<SenseTranslation> SenseTranslations => Set<SenseTranslation>();
    public DbSet<LearnerWord> LearnerWords => Set<LearnerWord>();
    public DbSet<LessonSense> LessonSenses => Set<LessonSense>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContentDbContext).Assembly);
    }
}
