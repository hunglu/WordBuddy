using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Quiz.Application.Interfaces;
using WordBuddy.Quiz.Infrastructure.Persistence;
using WordBuddy.Quiz.Infrastructure.Repositories;

namespace WordBuddy.Quiz.Infrastructure.Extensions;

/// <summary>Registers the <see cref="QuizDbContext"/> and repositories with the DI container.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<QuizDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                   .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        services.AddScoped<IQuizRepository, QuizRepository>();

        return services;
    }
}
