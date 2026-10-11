using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Infrastructure.ContentClient;
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
        services.AddScoped<ISupportLinkProjectionRepository, SupportLinkProjectionRepository>();
        services.AddScoped<ILearnerGroupProjectionRepository, LearnerGroupProjectionRepository>();
        services.AddScoped<IVocabularySessionIssueRepository, VocabularySessionIssueRepository>();
        services.AddScoped<IDashboardReadRepository, DashboardReadRepository>();

        services.AddScoped<IVocabularyExerciseRepository, VocabularyExerciseRepository>();

        AddContentClient(services, configuration);

        // No Redis instance exists in this repo yet; the in-memory IDistributedCache satisfies the
        // caching convention (dashboard). Swapping in Redis later is a DI-only change.
        services.AddDistributedMemoryCache();

        // Consumer side: Content LearnerWord events and Identity support-link events, deduped by the EF Core inbox.
        services.AddWordBuddyMessaging<ProgressDbContext>(configuration, bus =>
        {
            bus.AddConsumer<LearnerWordAddedConsumer>();
            bus.AddConsumer<LearnerWordRemovedConsumer>();
            bus.AddConsumer<SupportLinkActivatedConsumer>().Endpoint(e => e.Name = SupportLinkQueues.Activated);
            bus.AddConsumer<SupportLinkRevokedConsumer>().Endpoint(e => e.Name = SupportLinkQueues.Revoked);
            bus.AddConsumer<LearnerGroupMemberActivatedConsumer>().Endpoint(e => e.Name = LearnerGroupQueues.MemberActivated);
            bus.AddConsumer<LearnerGroupMemberRemovedConsumer>().Endpoint(e => e.Name = LearnerGroupQueues.MemberRemoved);
            bus.AddConsumer<LearnerGroupDeletedConsumer>().Endpoint(e => e.Name = LearnerGroupQueues.GroupDeleted);
        });

        return services;
    }

    /// <summary>
    /// Registers the Content sense client (caller's bearer token forwarded, standard resilience) and the
    /// named client used by <see cref="ContentHealthCheck"/>. Base URL: <c>Services:Content:BaseUrl</c>.
    /// </summary>
    private static void AddContentClient(IServiceCollection services, IConfiguration configuration)
    {
        string baseUrl = configuration[$"{ContentOptions.SectionName}:BaseUrl"] ?? ContentOptions.DefaultBaseUrl;
        Uri baseAddress = new(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");

        services.AddHttpContextAccessor();
        services.AddTransient<ForwardBearerTokenHandler>();

        services.AddHttpClient<IContentSenseClient, ContentSenseClient>(client => client.BaseAddress = baseAddress)
            .AddHttpMessageHandler<ForwardBearerTokenHandler>()
            .AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(4);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(12);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            });

        services.AddHttpClient(ContentHealthCheck.HttpClientName, client =>
        {
            client.BaseAddress = baseAddress;
            client.Timeout = TimeSpan.FromSeconds(3);
        });
    }
}
