using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using WordBuddy.Shared.Infrastructure.Health;

namespace WordBuddy.Shared.Infrastructure.Messaging;

/// <summary>
/// One shared MassTransit setup for every WordBuddy service: RabbitMQ transport, EF Core
/// transactional outbox (publisher side) and inbox (consumer side, dedupe on <c>MessageId</c>),
/// retry, kebab-case endpoint names and a broker health check tagged
/// <see cref="HealthCheckExtensions.ReadyTag"/>.
/// </summary>
public static class MessagingExtensions
{
    /// <summary>Configuration key that selects the transport.</summary>
    public const string TransportKey = "Messaging:Transport";

    /// <summary>Default transport: RabbitMQ.</summary>
    public const string RabbitMqTransport = "RabbitMq";

    /// <summary>In-memory transport with the MassTransit test harness. For tests only.</summary>
    public const string InMemoryTransport = "InMemory";

    /// <summary>Name of the broker health check.</summary>
    public const string HealthCheckName = "masstransit-bus";

    /// <summary>
    /// Registers MassTransit with the EF Core outbox/inbox on <typeparamref name="TDbContext"/>.
    /// Settings come from the validated <see cref="MessagingOptions"/> (<c>Messaging</c> section); bad values fail at startup.
    /// Outside Development, RabbitMQ <c>Username</c>/<c>Password</c> are required and must come from
    /// environment variables or user-secrets, never from a file in git.
    /// Set <c>Messaging:Transport = InMemory</c> in tests to use the in-memory test harness.
    /// </summary>
    /// <typeparam name="TDbContext">The service's own DbContext; it must call
    /// <see cref="AddWordBuddyMessagingEntities"/> in <c>OnModelCreating</c>.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="configureConsumers">Optional consumer registrations (consumer side only).</param>
    public static IServiceCollection AddWordBuddyMessaging<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureConsumers = null)
        where TDbContext : DbContext
    {
        services.AddSingleton<IValidateOptions<MessagingOptions>, MessagingOptionsValidator>();
        services.AddOptions<MessagingOptions>()
            .Bind(configuration.GetSection(MessagingOptions.SectionName))
            .ValidateOnStart();

        // Registration-time values (transport, outbox) need an eager copy; the rest is read
        // from IOptions<MessagingOptions> when the bus starts.
        MessagingOptions eager = new();
        configuration.GetSection(MessagingOptions.SectionName).Bind(eager);

        if (string.Equals(eager.Transport, InMemoryTransport, StringComparison.OrdinalIgnoreCase))
        {
            services.AddMassTransitTestHarness(bus => ConfigureBus<TDbContext>(bus, eager, configureConsumers));
            return services;
        }

        if (!string.Equals(eager.Transport, RabbitMqTransport, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unknown '{TransportKey}' value '{eager.Transport}'. Use '{RabbitMqTransport}' or '{InMemoryTransport}'.");
        }

        services.AddMassTransit(bus =>
        {
            ConfigureBus<TDbContext>(bus, eager, configureConsumers);

            bus.UsingRabbitMq((context, cfg) =>
            {
                MessagingOptions options = context.GetRequiredService<IOptions<MessagingOptions>>().Value;
                IHostEnvironment env = context.GetRequiredService<IHostEnvironment>();
                RabbitMqOptions rabbit = options.RabbitMq;

                // guest/guest is the RabbitMQ default account; allowed only in Development.
                string username = rabbit.Username ?? (env.IsDevelopment() ? "guest" : string.Empty);
                string password = rabbit.Password ?? (env.IsDevelopment() ? "guest" : string.Empty);

                cfg.Host(rabbit.Host, rabbit.VirtualHost, h =>
                {
                    h.Username(username);
                    h.Password(password);
                    h.Heartbeat(TimeSpan.FromSeconds(rabbit.HeartbeatSeconds));
                    h.RequestedConnectionTimeout(TimeSpan.FromMilliseconds(rabbit.RequestedConnectionTimeoutMs));
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    private static void ConfigureBus<TDbContext>(
        IBusRegistrationConfigurator bus,
        MessagingOptions eager,
        Action<IBusRegistrationConfigurator>? configureConsumers)
        where TDbContext : DbContext
    {
        bus.SetKebabCaseEndpointNameFormatter();

        bus.AddEntityFrameworkOutbox<TDbContext>(outbox =>
        {
            outbox.UseSqlServer();
            outbox.UseBusOutbox();
            outbox.QueryDelay = TimeSpan.FromMilliseconds(eager.Outbox.QueryDelayMs);
            outbox.DuplicateDetectionWindow = TimeSpan.FromSeconds(eager.Outbox.DuplicateDetectionWindowSeconds);
        });

        bus.AddConfigureEndpointsCallback((context, _, endpoint) =>
        {
            MessagingOptions options = context.GetRequiredService<IOptions<MessagingOptions>>().Value;
            TimeSpan[] intervals = options.Retry.IntervalsMs.Select(ms => TimeSpan.FromMilliseconds(ms)).ToArray();

            // Retry wraps the inbox so a failed attempt rolls back and is tried again.
            endpoint.UseMessageRetry(retry => retry.Intervals(intervals));
            endpoint.UseEntityFrameworkOutbox<TDbContext>(context);
        });

        bus.ConfigureHealthCheckOptions(options =>
        {
            options.Name = HealthCheckName;
            options.Tags.Add(HealthCheckExtensions.ReadyTag);
        });

        configureConsumers?.Invoke(bus);
    }

    /// <summary>
    /// Maps the MassTransit <c>InboxState</c>, <c>OutboxMessage</c> and <c>OutboxState</c> tables.
    /// Call from the service DbContext's <c>OnModelCreating</c>.
    /// </summary>
    public static ModelBuilder AddWordBuddyMessagingEntities(this ModelBuilder modelBuilder)
    {
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
        return modelBuilder;
    }
}
