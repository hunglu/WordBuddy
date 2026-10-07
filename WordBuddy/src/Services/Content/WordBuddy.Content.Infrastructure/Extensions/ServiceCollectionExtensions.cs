using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Infrastructure.Messaging;
using WordBuddy.Content.Infrastructure.Persistence;
using WordBuddy.Content.Infrastructure.Repositories;
using WordBuddy.Content.Infrastructure.Services;
using WordBuddy.Content.Infrastructure.Settings;
using WordBuddy.Shared.Infrastructure.Messaging;

namespace WordBuddy.Content.Infrastructure.Extensions;

/// <summary>Registers the <see cref="ContentDbContext"/>, repositories, and services with the DI container.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ContentDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                   .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        // Publisher only: LearnerWord changes go out through the EF Core outbox (ContentDbContext).
        services.AddWordBuddyMessaging<ContentDbContext>(configuration);

        services.Configure<FileStorageSettings>(configuration.GetSection("FileStorage"));

        services.AddScoped<ILessonRepository, LessonRepository>();
        services.AddScoped<IMediaAssetRepository, MediaAssetRepository>();
        services.AddScoped<IVocabularyWordRepository, VocabularyWordRepository>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<ILearnerWordEventPublisher, OutboxLearnerWordEventPublisher>();

        // No Redis instance exists anywhere in this repo yet (docker-compose has no `redis`
        // service, no service configures `IDistributedCache`) — registering the in-memory
        // implementation satisfies the `IDistributedCache` contract CLAUDE.md's caching
        // convention calls for without standing up new cross-cutting infrastructure as part of
        // this feature. Swapping in `AddStackExchangeRedisCache` later is a DI-only change; no
        // application code depends on which backend is registered here.
        services.AddDistributedMemoryCache();

        return services;
    }
}
