using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Features.Lessons.Commands.CreateLesson;
using WordBuddy.Content.Application.Features.Lessons.Queries.GetLessonDetail;
using WordBuddy.Content.Application.Features.Lessons.Queries.GetLessons;
using WordBuddy.Content.Application.Features.Media.Commands.UploadMedia;
using WordBuddy.Content.Application.Features.Media.Queries.GetMediaAsset;

namespace WordBuddy.Content.Application.Extensions;

/// <summary>Registers Content's Application-layer handlers and validators with the DI container.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<GetLessonsQuery, IReadOnlyList<LessonDto>>, GetLessonsQueryHandler>();
        services.AddScoped<IQueryHandler<GetLessonDetailQuery, LessonDetailDto>, GetLessonDetailQueryHandler>();

        services.AddScoped<ICommandHandler<CreateLessonCommand, Guid>, CreateLessonCommandHandler>();
        services.AddScoped<IValidator<CreateLessonCommand>, CreateLessonCommandValidator>();

        services.AddScoped<ICommandHandler<UploadMediaCommand, MediaAssetDto>, UploadMediaCommandHandler>();
        services.AddScoped<IValidator<UploadMediaCommand>, UploadMediaCommandValidator>();

        services.AddScoped<IQueryHandler<GetMediaAssetQuery, MediaAssetDto>, GetMediaAssetQueryHandler>();

        return services;
    }
}
