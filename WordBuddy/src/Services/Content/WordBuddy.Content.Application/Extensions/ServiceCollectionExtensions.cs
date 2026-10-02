using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Features.Lessons.Commands.CreateLesson;
using WordBuddy.Content.Application.Features.Lessons.Queries.GetLessonDetail;
using WordBuddy.Content.Application.Features.Lessons.Queries.GetLessons;
using WordBuddy.Content.Application.Features.Media.Commands.UploadMedia;
using WordBuddy.Content.Application.Features.Media.Queries.GetMediaAsset;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.AddPersonalVocabularyWord;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.AddSharedVocabularyWordToMyList;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.DeletePersonalVocabularyWord;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.ModerateSharedVocabularyWord;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.RequestShareVocabularyWord;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetMyVocabularyWords;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetPendingVocabularyModeration;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetRandomVocabularyWordsForCheck;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetSharedVocabularyWords;
using WordBuddy.Content.Application.Features.VocabularyRemaps.Commands.AcknowledgeVocabularyWordIdRemaps;
using WordBuddy.Content.Application.Features.VocabularyRemaps.Queries.GetPendingVocabularyWordIdRemaps;

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

        services.AddScoped<ICommandHandler<AddPersonalVocabularyWordCommand, Guid>, AddPersonalVocabularyWordCommandHandler>();
        services.AddScoped<IValidator<AddPersonalVocabularyWordCommand>, AddPersonalVocabularyWordCommandValidator>();

        services.AddScoped<ICommandHandler<RequestShareVocabularyWordCommand>, RequestShareVocabularyWordCommandHandler>();
        services.AddScoped<IValidator<RequestShareVocabularyWordCommand>, RequestShareVocabularyWordCommandValidator>();

        services.AddScoped<ICommandHandler<ModerateSharedVocabularyWordCommand>, ModerateSharedVocabularyWordCommandHandler>();
        services.AddScoped<IValidator<ModerateSharedVocabularyWordCommand>, ModerateSharedVocabularyWordCommandValidator>();

        services.AddScoped<ICommandHandler<AddSharedVocabularyWordToMyListCommand, Guid>, AddSharedVocabularyWordToMyListCommandHandler>();
        services.AddScoped<IValidator<AddSharedVocabularyWordToMyListCommand>, AddSharedVocabularyWordToMyListCommandValidator>();

        services.AddScoped<ICommandHandler<DeletePersonalVocabularyWordCommand>, DeletePersonalVocabularyWordCommandHandler>();
        services.AddScoped<IValidator<DeletePersonalVocabularyWordCommand>, DeletePersonalVocabularyWordCommandValidator>();

        services.AddScoped<IQueryHandler<GetMyVocabularyWordsQuery, IReadOnlyList<PersonalVocabularyWordDto>>, GetMyVocabularyWordsQueryHandler>();
        services.AddScoped<IQueryHandler<GetSharedVocabularyWordsQuery, IReadOnlyList<PersonalVocabularyWordDto>>, GetSharedVocabularyWordsQueryHandler>();

        services.AddScoped<IQueryHandler<GetRandomVocabularyWordsForCheckQuery, IReadOnlyList<PersonalVocabularyWordDto>>, GetRandomVocabularyWordsForCheckQueryHandler>();
        services.AddScoped<IValidator<GetRandomVocabularyWordsForCheckQuery>, GetRandomVocabularyWordsForCheckQueryValidator>();

        services.AddScoped<IQueryHandler<GetPendingVocabularyModerationQuery, IReadOnlyList<PersonalVocabularyWordDto>>, GetPendingVocabularyModerationQueryHandler>();

        services.AddScoped<IQueryHandler<GetPendingVocabularyWordIdRemapsQuery, IReadOnlyList<VocabularyWordIdRemapDto>>, GetPendingVocabularyWordIdRemapsQueryHandler>();
        services.AddScoped<IValidator<GetPendingVocabularyWordIdRemapsQuery>, GetPendingVocabularyWordIdRemapsQueryValidator>();

        services.AddScoped<ICommandHandler<AcknowledgeVocabularyWordIdRemapsCommand>, AcknowledgeVocabularyWordIdRemapsCommandHandler>();
        services.AddScoped<IValidator<AcknowledgeVocabularyWordIdRemapsCommand>, AcknowledgeVocabularyWordIdRemapsCommandValidator>();

        return services;
    }
}
