using Grpc.Health.V1;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Infrastructure.Challenges;
using WordBuddy.Progress.Infrastructure.ContentClient;
using WordBuddy.Progress.Infrastructure.Messaging;
using WordBuddy.Progress.Infrastructure.Persistence;
using WordBuddy.Progress.Infrastructure.Repositories;
using WordBuddy.Shared.Infrastructure.Messaging;
using WordBuddy.Shared.Protos.Content.V1;

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
        services.AddScoped<IChallengeRepository, ChallengeRepository>();
        services.AddScoped<IChallengeProfileRepository, ChallengeProfileRepository>();

        AddContentClient(services, configuration);

        // Challenge clock (WB-29). ChallengeOptions is registered by the host (AddChallenges).
        services.AddHostedService<ChallengeSchedulerHostedService>();

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
            bus.AddConsumer<LearnerProfileUpdatedConsumer>().Endpoint(e => e.Name = LearnerProfileQueues.Updated);
            bus.AddConsumer<LearnerGroupMemberActivatedConsumer>().Endpoint(e => e.Name = LearnerGroupQueues.MemberActivated);
            bus.AddConsumer<LearnerGroupMemberRemovedConsumer>().Endpoint(e => e.Name = LearnerGroupQueues.MemberRemoved);
            bus.AddConsumer<LearnerGroupDeletedConsumer>().Endpoint(e => e.Name = LearnerGroupQueues.GroupDeleted);
        });

        return services;
    }

    /// <summary>
    /// Registers the Content sense client for the transport in <c>Services:Content:Transport</c> (default
    /// <c>Grpc</c>; <c>Rest</c> is the explicit alternative), plus the named client used by
    /// <see cref="ContentHealthCheck"/>. Both transports forward the caller's bearer token and use the
    /// same standard resilience settings. Switching is a config change and a restart, no fallback at runtime.
    /// </summary>
    internal static void AddContentClient(IServiceCollection services, IConfiguration configuration)
    {
        ContentOptions options = ContentOptions.FromConfiguration(configuration);
        Uri baseAddress = new(options.BaseUrl.EndsWith('/') ? options.BaseUrl : options.BaseUrl + "/");

        services.AddHttpContextAccessor();
        services.AddTransient<ForwardBearerTokenHandler>();
        services.AddTransient<SessionSenseCache>();

        if (options.Transport == ContentTransport.Rest)
        {
            services.AddHttpClient<IContentSenseClient, ContentSenseClient>(client => client.BaseAddress = baseAddress)
                .AddHttpMessageHandler<ForwardBearerTokenHandler>()
                .AddStandardResilienceHandler(ConfigureResilience);
        }
        else
        {
            Uri grpcAddress = new(options.GrpcUrl);

            services.AddGrpcClient<ContentSenses.ContentSensesClient>(client => client.Address = grpcAddress)
                .AddHttpMessageHandler<ForwardBearerTokenHandler>()
                .AddStandardResilienceHandler(ConfigureResilience);
            services.AddTransient<IContentSenseClient, ContentSenseGrpcClient>();

            // grpc.health.v1 client for ContentGrpcHealthCheck. No token: the health service is anonymous.
            services.AddGrpcClient<Health.HealthClient>(client => client.Address = grpcAddress);
        }

        services.AddHttpClient(ContentHealthCheck.HttpClientName, client =>
        {
            client.BaseAddress = baseAddress;
            client.Timeout = TimeSpan.FromSeconds(3);
        });
    }

    private static void ConfigureResilience(HttpStandardResilienceOptions options)
    {
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(4);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(12);
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    }
}
