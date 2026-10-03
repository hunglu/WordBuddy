using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Infrastructure.Persistence;
using WordBuddy.Progress.Infrastructure.Repositories;

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

        return services;
    }
}
