using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Infrastructure.Persistence;
using WordBuddy.Progress.Infrastructure.Repositories;
using WordBuddy.Progress.Infrastructure.Services;
using WordBuddy.Progress.Infrastructure.Settings;

namespace WordBuddy.Progress.Infrastructure.Extensions;

/// <summary>Registers the <see cref="ProgressDbContext"/>, repositories, and the Content remap sync
/// with the DI container.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ProgressDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                   .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        services.AddScoped<ILearnerProgressRepository, LearnerProgressRepository>();
        services.AddScoped<IVocabularyRecallRepository, VocabularyRecallRepository>();

        services.AddContentRemapSync(configuration);

        return services;
    }

    /// <summary>Typed Content client (service JWT + standard resilience) and the background sync that
    /// pulls vocabulary word-id remaps from it.</summary>
    private static IServiceCollection AddContentRemapSync(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ContentApiSettings>(configuration.GetSection(ContentApiSettings.SectionName));
        services.TryAddSingleton(TimeProvider.System);

        services.AddSingleton(sp =>
        {
            IConfiguration config = sp.GetRequiredService<IConfiguration>();
            string secret = config["Jwt:Secret"]
                ?? throw new InvalidOperationException("'Jwt:Secret' configuration is required.");
            string issuer = config["Jwt:Issuer"]
                ?? throw new InvalidOperationException("'Jwt:Issuer' configuration is required.");
            return new ServiceTokenProvider(secret, issuer, sp.GetRequiredService<TimeProvider>());
        });
        services.AddTransient<ServiceTokenHandler>();

        services.AddHttpClient<IContentVocabularyRemapClient, ContentVocabularyRemapClient>(
                ContentVocabularyRemapClient.HttpClientName,
                (sp, client) =>
                {
                    ContentApiSettings settings = sp.GetRequiredService<IOptions<ContentApiSettings>>().Value;
                    client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
                })
            .AddHttpMessageHandler<ServiceTokenHandler>()
            .AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.BackoffType = DelayBackoffType.Exponential;

                // 5xx, 408 and transient network failures/timeouts only — never 401/403/404 (or 429).
                options.Retry.ShouldHandle = args => ValueTask.FromResult(args.Outcome switch
                {
                    { Exception: HttpRequestException } => true,
                    { Exception: Polly.Timeout.TimeoutRejectedException } => true,
                    { Result: { } response } =>
                        (int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.RequestTimeout,
                    _ => false,
                });
            });

        services.AddHostedService<VocabularyIdRemapSyncService>();

        return services;
    }
}
