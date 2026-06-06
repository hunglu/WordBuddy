using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Application.Interfaces;
using WordBuddy.Infrastructure.Persistence;
using WordBuddy.Infrastructure.Persistence.Repositories;
using WordBuddy.Infrastructure.Services;

namespace WordBuddy.Infrastructure.Extensions;

/// <summary>Extension methods for registering Infrastructure services.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="WordBuddyDbContext"/>, repositories, and all Infrastructure
    /// dependencies with the DI container.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configuration">The application configuration (reads <c>ConnectionStrings:DefaultConnection</c>).</param>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<WordBuddyDbContext>(options =>
            options
                .UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        services.AddScoped<ILessonRepository, LessonRepository>();
        services.AddScoped<ILearnerProgressRepository, LearnerProgressRepository>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
