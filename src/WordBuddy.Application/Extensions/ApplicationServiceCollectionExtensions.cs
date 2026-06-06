using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Application.Features.Lessons.Commands.CreateLesson;
using WordBuddy.Application.Features.Lessons.Queries.GetLessonDetail;
using WordBuddy.Application.Features.Lessons.Queries.GetLessons;
using WordBuddy.Application.Features.Progress.Commands.RecordProgress;
using WordBuddy.Application.Features.Progress.Queries.GetUserProgress;

namespace WordBuddy.Application.Extensions;

/// <summary>Extension methods for registering Application-layer services with the DI container.</summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Application-layer query/command handlers and FluentValidation validators.
    /// Call this from the composition root (<c>Program.cs</c>) alongside <c>AddInfrastructure</c>.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(
            typeof(ApplicationServiceCollectionExtensions).Assembly,
            includeInternalTypes: true);

        services.AddScoped<GetLessonsQueryHandler>();
        services.AddScoped<GetLessonDetailQueryHandler>();
        services.AddScoped<CreateLessonCommandHandler>();
        services.AddScoped<RecordProgressCommandHandler>();
        services.AddScoped<GetUserProgressQueryHandler>();

        return services;
    }
}
