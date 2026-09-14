using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Features.Progress.Commands.RecordProgress;
using WordBuddy.Progress.Application.Features.Progress.Queries.GetUserProgress;

namespace WordBuddy.Progress.Application.Extensions;

/// <summary>Registers Progress's Application-layer handlers and validators with the DI container.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<RecordProgressCommand>, RecordProgressCommandHandler>();
        services.AddScoped<IValidator<RecordProgressCommand>, RecordProgressCommandValidator>();

        services.AddScoped<IQueryHandler<GetUserProgressQuery, IReadOnlyList<LearnerProgressDto>>, GetUserProgressQueryHandler>();

        return services;
    }
}
