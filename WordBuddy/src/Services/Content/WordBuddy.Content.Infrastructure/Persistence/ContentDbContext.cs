using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Content.Domain;
using WordBuddy.Content.Infrastructure.Messaging;
using WordBuddy.Shared.Infrastructure.Messaging;

namespace WordBuddy.Content.Infrastructure.Persistence;

/// <summary>EF Core context for the Content service's own database (<c>WordBuddyContent</c>).
/// Every <see cref="LearnerWord"/> insert/delete publishes an event through the EF Core outbox,
/// in the same transaction as the change.</summary>
public sealed class ContentDbContext : DbContext
{
    private readonly IServiceProvider? _serviceProvider;

    /// <summary>Design-time / test constructor: no events are published.</summary>
    public ContentDbContext(DbContextOptions<ContentDbContext> options) : base(options)
    {
    }

    /// <summary>Runtime constructor: <paramref name="serviceProvider"/> (the request scope) resolves
    /// the outbox publish endpoint lazily, because that endpoint itself depends on this context.</summary>
    public ContentDbContext(DbContextOptions<ContentDbContext> options, IServiceProvider serviceProvider) : base(options)
    {
        _serviceProvider = serviceProvider;
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
    public DbSet<SupportLinkProjection> SupportLinkProjections => Set<SupportLinkProjection>();
    public DbSet<LearnerGroupMemberProjection> LearnerGroupMemberProjections => Set<LearnerGroupMemberProjection>();
    public DbSet<GroupWordAssignment> GroupWordAssignments => Set<GroupWordAssignment>();

    /// <inheritdoc />
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        IPublishEndpoint? publishEndpoint = _serviceProvider?.GetService<IPublishEndpoint>();
        if (publishEndpoint is null)
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        IReadOnlyList<object> events = LearnerWordEventCollector.Collect(ChangeTracker, DateTime.UtcNow);
        if (events.Count == 0)
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        HashSet<OutboxMessage> before = ChangeTracker.Entries<OutboxMessage>().Select(e => e.Entity).ToHashSet();

        // The bus outbox adds an OutboxMessage row per event to this context — saved below,
        // in the same transaction as the link change.
        foreach (object message in events)
        {
            await publishEndpoint.Publish(message, message.GetType(), cancellationToken);
        }

        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The link change failed (e.g. a concurrent duplicate): drop its outbox rows so a retry
            // or a later save never sends an event for a change that did not happen.
            foreach (EntityEntry<OutboxMessage> entry in ChangeTracker.Entries<OutboxMessage>().ToList())
            {
                if (!before.Contains(entry.Entity))
                {
                    entry.State = EntityState.Detached;
                }
            }

            throw;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContentDbContext).Assembly);
        modelBuilder.AddWordBuddyMessagingEntities();
    }
}
