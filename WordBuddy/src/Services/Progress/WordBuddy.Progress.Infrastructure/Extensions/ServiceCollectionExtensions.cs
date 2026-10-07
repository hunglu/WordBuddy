using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Infrastructure.Messaging;
using WordBuddy.Progress.Infrastructure.Persistence;
using WordBuddy.Progress.Infrastructure.Repositories;
using WordBuddy.Shared.Infrastructure.Messaging;

namespace WordBuddy.Progress.Infrastructure.Extensions;

/// <summary>Registers the <see cref="ProgressDbContext"/> and repositories with the DI container.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ProgressDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                   .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        services.AddScoped<ILearnerProgressRepository, LearnerProgressRepository>();
        services.AddScoped<IVocabularyRecallRepository, VocabularyRecallRepository>();
        services.AddScoped<ILearnerWordMembershipRepository, LearnerWordMembershipRepository>();
        services.AddScoped<ILearnerWordStateRepository, LearnerWordStateRepository>();
        services.AddScoped<IReviewLogRepository, ReviewLogRepository>();
        services.AddScoped<IVocabularyLearnerSettingsRepository, VocabularyLearnerSettingsRepository>();

        // Consumer side: Content's LearnerWord events, deduped by the EF Core inbox.
        services.AddWordBuddyMessaging<ProgressDbContext>(configuration, bus =>
        {
            bus.AddConsumer<LearnerWordAddedConsumer>();
            bus.AddConsumer<LearnerWordRemovedConsumer>();
        });

        return services;
    }
}
