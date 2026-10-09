using FluentValidation;
using WordBuddy.Progress.Application.Features.Dashboard.Queries.GetLearnerDashboard;
using WordBuddy.Progress.Application.Features.SupportLinks.Commands.ApplySupportLinkEvent;
using WordBuddy.Progress.Application.Features.VocabularySrs.Commands.UpdateLearnerVocabularySettings;
using WordBuddy.Progress.Application.Features.VocabularySrs.Queries.GetLearnerVocabularySettings;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Features.LearnerWords.Commands.RecordLearnerWordAdded;
using WordBuddy.Progress.Application.Features.LearnerWords.Commands.RecordLearnerWordRemoved;
using WordBuddy.Progress.Application.Features.Progress.Commands.RecordProgress;
using WordBuddy.Progress.Application.Features.Progress.Queries.GetUserProgress;
using WordBuddy.Progress.Application.Features.VocabularyRecall.Commands.SubmitVocabularyRecallCheck;
using WordBuddy.Progress.Application.Features.VocabularyRecall.Queries.GetVocabularyRecallProgress;
using WordBuddy.Progress.Application.Features.VocabularySrs.Commands.RecordVocabularyReview;
using WordBuddy.Progress.Application.Features.VocabularySrs.Commands.UpdateVocabularySettings;
using WordBuddy.Progress.Application.Features.VocabularySrs.Queries.GetLearnerWordStates;
using WordBuddy.Progress.Application.Features.VocabularySrs.Queries.GetVocabularySession;
using WordBuddy.Progress.Application.Features.VocabularySrs.Queries.GetVocabularySettings;

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

        // Vocabulary SRS (WB-22). Options and domain services are registered by the host.
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<ICommandHandler<RecordVocabularyReviewCommand, VocabularyReviewResultDto>, RecordVocabularyReviewCommandHandler>();
        services.AddScoped<IValidator<RecordVocabularyReviewCommand>, RecordVocabularyReviewCommandValidator>();

        services.AddScoped<IQueryHandler<GetVocabularySessionQuery, VocabularySessionDto>, GetVocabularySessionQueryHandler>();
        services.AddScoped<IQueryHandler<GetLearnerWordStatesQuery, IReadOnlyList<LearnerWordStateDto>>, GetLearnerWordStatesQueryHandler>();
        services.AddScoped<IQueryHandler<GetVocabularySettingsQuery, VocabularySettingsDto>, GetVocabularySettingsQueryHandler>();

        services.AddScoped<ICommandHandler<UpdateVocabularySettingsCommand, VocabularySettingsDto>, UpdateVocabularySettingsCommandHandler>();
        services.AddScoped<IValidator<UpdateVocabularySettingsCommand>, UpdateVocabularySettingsCommandValidator>();

        // Supporter access (WB-24)
        services.AddScoped<IQueryHandler<GetLearnerVocabularySettingsQuery, LearnerVocabularySettingsDto>, GetLearnerVocabularySettingsQueryHandler>();
        services.AddScoped<ICommandHandler<UpdateLearnerVocabularySettingsCommand, LearnerVocabularySettingsDto>, UpdateLearnerVocabularySettingsCommandHandler>();
        services.AddScoped<IValidator<UpdateLearnerVocabularySettingsCommand>, UpdateLearnerVocabularySettingsCommandValidator>();
        services.AddScoped<ICommandHandler<ApplySupportLinkEventCommand>, ApplySupportLinkEventCommandHandler>();
        services.AddScoped<IValidator<ApplySupportLinkEventCommand>, ApplySupportLinkEventCommandValidator>();

        // Dashboard (WB-26). DashboardOptions and DashboardCalculator are registered by the host.
        services.AddScoped<IQueryHandler<GetLearnerDashboardQuery, LearnerDashboardDto>, GetLearnerDashboardQueryHandler>();
        services.AddScoped<IValidator<GetLearnerDashboardQuery>, GetLearnerDashboardQueryValidator>();

        return services;
    }
}
