using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Features.LearnerWords.Commands.RecordLearnerWordAdded;
using WordBuddy.Progress.Application.Features.LearnerWords.Commands.RecordLearnerWordRemoved;
using WordBuddy.Progress.Application.Features.Progress.Commands.RecordProgress;
using WordBuddy.Progress.Application.Features.Progress.Queries.GetUserProgress;
using WordBuddy.Progress.Application.Features.VocabularyRecall.Commands.SubmitVocabularyRecallCheck;
using WordBuddy.Progress.Application.Features.VocabularyRecall.Queries.GetVocabularyRecallProgress;

namespace WordBuddy.Progress.Application.Extensions;

/// <summary>Registers Progress's Application-layer handlers and validators with the DI container.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<RecordProgressCommand>, RecordProgressCommandHandler>();
        services.AddScoped<IValidator<RecordProgressCommand>, RecordProgressCommandValidator>();

        services.AddScoped<IQueryHandler<GetUserProgressQuery, IReadOnlyList<LearnerProgressDto>>, GetUserProgressQueryHandler>();

        services.AddScoped<ICommandHandler<SubmitVocabularyRecallCheckCommand>, SubmitVocabularyRecallCheckCommandHandler>();
        services.AddScoped<IValidator<SubmitVocabularyRecallCheckCommand>, SubmitVocabularyRecallCheckCommandValidator>();

        services.AddScoped<IQueryHandler<GetVocabularyRecallProgressQuery, VocabularyRecallProgressDto>, GetVocabularyRecallProgressQueryHandler>();

        services.AddScoped<ICommandHandler<RecordLearnerWordAddedCommand>, RecordLearnerWordAddedCommandHandler>();
        services.AddScoped<IValidator<RecordLearnerWordAddedCommand>, RecordLearnerWordAddedCommandValidator>();

        services.AddScoped<ICommandHandler<RecordLearnerWordRemovedCommand>, RecordLearnerWordRemovedCommandHandler>();
        services.AddScoped<IValidator<RecordLearnerWordRemovedCommand>, RecordLearnerWordRemovedCommandValidator>();

        return services;
    }
}
