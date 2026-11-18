using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Quiz.Application.Interfaces;
using WordBuddy.Quiz.Infrastructure.Persistence;
using WordBuddy.Quiz.Infrastructure.Persistence.Postgres;
using WordBuddy.Quiz.Infrastructure.Persistence.SqlServer;
using WordBuddy.Quiz.Infrastructure.Repositories;

namespace WordBuddy.Quiz.Infrastructure.Extensions;

/// <summary>Registers the <see cref="QuizDbContext"/> and repositories with the DI container.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        DatabaseOptions database = DatabaseOptions.FromConfiguration(configuration);
        string? connectionString = configuration.GetConnectionString("DefaultConnection");

        // Repositories, health checks and the seeder keep resolving QuizDbContext; only the subclass differs.
        if (database.Provider == DatabaseProvider.Postgres)
        {
            services.AddDbContext<QuizDbContext, PostgresQuizDbContext>(options =>
                options.UseNpgsql(connectionString)
                       .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
        }
        else
        {
            services.AddDbContext<QuizDbContext, SqlServerQuizDbContext>(options =>
                options.UseSqlServer(connectionString)
                       .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
        }

        services.AddScoped<IQuizRepository, QuizRepository>();

        return services;
    }
}
