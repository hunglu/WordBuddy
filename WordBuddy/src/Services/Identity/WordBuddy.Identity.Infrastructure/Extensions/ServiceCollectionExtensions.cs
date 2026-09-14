using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Infrastructure.Persistence;
using WordBuddy.Identity.Infrastructure.Repositories;
using WordBuddy.Identity.Infrastructure.Services;
using WordBuddy.Identity.Infrastructure.Settings;

namespace WordBuddy.Identity.Infrastructure.Extensions;

/// <summary>Registers the <see cref="IdentityDbContext"/>, repositories, and services with the DI container.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                   .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
