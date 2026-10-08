using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Application.Settings;
using WordBuddy.Identity.Infrastructure.Messaging;
using WordBuddy.Identity.Infrastructure.Persistence;
using WordBuddy.Identity.Infrastructure.Repositories;
using WordBuddy.Identity.Infrastructure.Services;
using WordBuddy.Identity.Infrastructure.Settings;
using WordBuddy.Shared.Infrastructure.Messaging;

namespace WordBuddy.Identity.Infrastructure.Extensions;

/// <summary>Registers the <see cref="IdentityDbContext"/>, repositories, and services with the DI container.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                   .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        // Publisher only: support-link events go out through the EF Core outbox (IdentityDbContext).
        services.AddWordBuddyMessaging<IdentityDbContext>(configuration);

        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

        SupportLinkOptions supportLinkOptions =
            configuration.GetSection(SupportLinkOptions.SectionName).Get<SupportLinkOptions>() ?? new();
        services.AddSingleton(supportLinkOptions);
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISupportLinkRepository, SupportLinkRepository>();
        services.AddScoped<ISupportLinkEventPublisher, OutboxSupportLinkEventPublisher>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        // Same as Content: no Redis in this repo yet; the in-memory IDistributedCache keeps the
        // caching contract and swapping to Redis later is a DI-only change.
        services.AddDistributedMemoryCache();

        return services;
    }
}
