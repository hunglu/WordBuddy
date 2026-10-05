using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    /// RabbitMQ settings come from <c>Messaging:RabbitMq:{Host,VirtualHost,Username,Password}</c>;
    /// the password must come from environment variables or user-secrets, never from a file in git.
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
        string transport = configuration[TransportKey] ?? RabbitMqTransport;

        if (string.Equals(transport, InMemoryTransport, StringComparison.OrdinalIgnoreCase))
        {
            services.AddMassTransitTestHarness(bus => ConfigureBus<TDbContext>(bus, configureConsumers));
            return services;
        }

        if (!string.Equals(transport, RabbitMqTransport, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unknown '{TransportKey}' value '{transport}'. Use '{RabbitMqTransport}' or '{InMemoryTransport}'.");
        }

        IConfigurationSection rabbit = configuration.GetSection("Messaging:RabbitMq");
        string host = rabbit["Host"] ?? "localhost";
        string virtualHost = rabbit["VirtualHost"] ?? "/";
        string username = rabbit["Username"] ?? "guest";
        string password = rabbit["Password"] ?? "guest";

        services.AddMassTransit(bus =>
        {
            ConfigureBus<TDbContext>(bus, configureConsumers);

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(host, virtualHost, h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    private static void ConfigureBus<TDbContext>(
        IBusRegistrationConfigurator bus,
        Action<IBusRegistrationConfigurator>? configureConsumers)
        where TDbContext : DbContext
    {
        bus.SetKebabCaseEndpointNameFormatter();

        bus.AddEntityFrameworkOutbox<TDbContext>(outbox =>
        {
            outbox.UseSqlServer();
            outbox.UseBusOutbox();
            outbox.QueryDelay = TimeSpan.FromSeconds(1);
        });

        bus.AddConfigureEndpointsCallback((context, _, endpoint) =>
        {
            // Retry wraps the inbox so a failed attempt rolls back and is tried again.
            endpoint.UseMessageRetry(retry => retry.Intervals(
                TimeSpan.FromMilliseconds(200),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(5)));
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
