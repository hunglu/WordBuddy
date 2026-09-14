using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Infrastructure.Persistence;
using WordBuddy.Content.Infrastructure.Repositories;
using WordBuddy.Content.Infrastructure.Services;
using WordBuddy.Content.Infrastructure.Settings;

namespace WordBuddy.Content.Infrastructure.Extensions;

/// <summary>Registers the <see cref="ContentDbContext"/>, repositories, and services with the DI container.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ContentDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                   .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        services.Configure<FileStorageSettings>(configuration.GetSection("FileStorage"));

        services.AddScoped<ILessonRepository, LessonRepository>();
        services.AddScoped<IMediaAssetRepository, MediaAssetRepository>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        return services;
    }
}
