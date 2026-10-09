using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Application.Interfaces.Autofill;
using WordBuddy.Content.Infrastructure.Autofill;
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
    /// <param name="isDevelopment">Host is in the Development environment. Only then may
    /// <c>Autofill:UseFakeClients</c> switch on the keyless fake auto-fill clients.</param>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, bool isDevelopment = false)
    {
        services.AddDbContext<ContentDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                   .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        // Publisher: LearnerWord changes go out through the EF Core outbox (ContentDbContext).
        // Consumer: Identity support-link events, deduped by the EF Core inbox.
        services.AddWordBuddyMessaging<ContentDbContext>(configuration, bus =>
        {
            bus.AddConsumer<SupportLinkActivatedConsumer>().Endpoint(e => e.Name = SupportLinkQueues.Activated);
            bus.AddConsumer<SupportLinkRevokedConsumer>().Endpoint(e => e.Name = SupportLinkQueues.Revoked);
        });

        services.Configure<FileStorageSettings>(configuration.GetSection("FileStorage"));

        services.AddScoped<ILessonRepository, LessonRepository>();
        services.AddScoped<IMediaAssetRepository, MediaAssetRepository>();
        services.AddScoped<IVocabularyWordRepository, VocabularyWordRepository>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<ILearnerWordEventPublisher, OutboxLearnerWordEventPublisher>();
        services.AddScoped<ISupportLinkProjectionRepository, SupportLinkProjectionRepository>();
        services.AddScoped<IAutofillRepository, AutofillRepository>();

        services.AddAutofillClients(configuration, isDevelopment);

        // No Redis instance exists anywhere in this repo yet (docker-compose has no `redis`
        // service, no service configures `IDistributedCache`) — registering the in-memory
        // implementation satisfies the `IDistributedCache` contract CLAUDE.md's caching
        // convention calls for without standing up new cross-cutting infrastructure as part of
        // this feature. Swapping in `AddStackExchangeRedisCache` later is a DI-only change; no
        // application code depends on which backend is registered here.
        services.AddDistributedMemoryCache();

        return services;
    }

    /// <summary>Registers the auto-fill settings and the three typed HTTP clients (dictionary,
    /// Claude, audio) with timeouts and the standard resilience handler. The Claude key is read
    /// from configuration at call time (user-secrets / environment only).</summary>
    public static IServiceCollection AddAutofillClients(this IServiceCollection services, IConfiguration configuration, bool isDevelopment = false)
    {
        IConfigurationSection section = configuration.GetSection(AutofillSettings.SectionName);
        services.Configure<AutofillClientSettings>(section);
        AutofillClientSettings settings = section.Get<AutofillClientSettings>() ?? new AutofillClientSettings();

        string[] locales = section.GetSection("TranslationLocales").Get<string[]>() ?? [];
        services.AddSingleton(new AutofillSettings { TranslationLocales = locales.Length > 0 ? locales : ["vi"] });

        if (UseFakeClients(configuration, isDevelopment))
        {
            // E2E only (e2e/docker-compose.e2e.yml): deterministic, keyless, no network.
            DevelopmentFakeAutofillClients fakes = new();
            services.AddSingleton<IDictionaryClient>(fakes);
            services.AddSingleton<ISenseGenerator>(fakes);
            services.AddSingleton<IAudioDownloader>(fakes);
            return services;
        }

        services.AddHttpClient<IDictionaryClient, FreeDictionaryClient>(client =>
                client.BaseAddress = new Uri(settings.Dictionary.BaseUrl))
            .AddStandardResilienceHandler(options => ConfigureTimeouts(options, settings.Dictionary.TimeoutSeconds, retry: true));

        services.AddHttpClient<ISenseGenerator, ClaudeSenseGenerator>(client =>
                client.BaseAddress = new Uri(settings.Claude.BaseUrl))
            .AddStandardResilienceHandler(options => ConfigureTimeouts(options, settings.Claude.TimeoutSeconds, retry: false));

        services.AddHttpClient<IAudioDownloader, HttpAudioDownloader>()
            .AddStandardResilienceHandler(options => ConfigureTimeouts(options, settings.Audio.TimeoutSeconds, retry: true));

        return services;
    }

    /// <summary>Config key of the fake-client switch (default <see langword="false"/>).</summary>
    public const string UseFakeClientsKey = "Autofill:UseFakeClients";

    /// <summary>True only when <see cref="UseFakeClientsKey"/> is <c>true</c> <b>and</b> the host is
    /// Development. Outside Development the flag is ignored and the real clients are used.</summary>
    public static bool UseFakeClients(IConfiguration configuration, bool isDevelopment) =>
        isDevelopment && configuration.GetValue<bool>(UseFakeClientsKey);

    /// <summary>Total timeout = <paramref name="seconds"/>. The Claude call is a paid POST, so it is
    /// never retried.</summary>
    private static void ConfigureTimeouts(HttpStandardResilienceOptions options, int seconds, bool retry)
    {
        TimeSpan total = TimeSpan.FromSeconds(Math.Max(1, seconds));
        options.TotalRequestTimeout.Timeout = total;
        options.AttemptTimeout.Timeout = retry ? TimeSpan.FromTicks(total.Ticks / 2) : total;
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromTicks(Math.Max(options.AttemptTimeout.Timeout.Ticks * 2, TimeSpan.FromSeconds(30).Ticks));
        if (!retry)
        {
            options.Retry.MaxRetryAttempts = 1;
            options.Retry.ShouldHandle = _ => ValueTask.FromResult(false);
        }
        else
        {
            options.Retry.MaxRetryAttempts = 1;
        }
    }
}
