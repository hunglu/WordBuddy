using FluentValidation;
using WordBuddy.Progress.Application.Features.Challenges;
using WordBuddy.Progress.Application.Features.Challenges.Commands.AdvanceChallengeLifecycle;
using WordBuddy.Progress.Application.Features.Challenges.Commands.ApplyLearnerProfileEvent;
using WordBuddy.Progress.Application.Features.Challenges.Commands.JoinChallenge;
using WordBuddy.Progress.Application.Features.Challenges.Commands.LeaveChallenge;
using WordBuddy.Progress.Application.Features.Challenges.Commands.RecomputeChallengeScores;
using WordBuddy.Progress.Application.Features.Challenges.Commands.SetChildChallengeConsent;
using WordBuddy.Progress.Application.Features.Challenges.Queries.GetChallengeBoard;
using WordBuddy.Progress.Application.Features.Challenges.Queries.ListChallenges;
using WordBuddy.Progress.Application.Features.Dashboard.Queries.GetLearnerDashboard;
using WordBuddy.Progress.Application.Features.Groups;
using WordBuddy.Progress.Application.Features.Groups.Commands.ApplyLearnerGroupEvent;
using WordBuddy.Progress.Application.Features.Groups.Queries.GetGroupDashboard;
using WordBuddy.Progress.Application.Features.SupportLinks.Commands.ApplySupportLinkEvent;
using WordBuddy.Progress.Application.Features.VocabularySrs.Commands.UpdateLearnerVocabularySettings;
using WordBuddy.Progress.Application.Features.VocabularySrs.Queries.GetLearnerVocabularySettings;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Features.LearnerWords.Commands.RecordLearnerWordAdded;
using WordBuddy.Progress.Application.Features.LearnerWords.Commands.RecordLearnerWordRemoved;
using WordBuddy.Progress.Application.Features.Progress.Commands.RecordProgress;
using WordBuddy.Progress.Application.Features.Progress.Queries.GetUserProgress;
using WordBuddy.Progress.Application.Features.VocabularyRecall.Commands.SubmitVocabularyRecallCheck;
using WordBuddy.Progress.Application.Features.VocabularyRecall.Queries.GetVocabularyRecallProgress;
using WordBuddy.Progress.Application.Features.VocabularySrs.Commands.CreateVocabularyExercise;
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

        // Server-side answer checking (WB-28). ResponseTimeEvaluator is registered by the host.
        services.AddScoped<ICommandHandler<CreateVocabularyExerciseCommand, VocabularyExerciseDto>, CreateVocabularyExerciseCommandHandler>();
        services.AddScoped<IValidator<CreateVocabularyExerciseCommand>, CreateVocabularyExerciseCommandValidator>();

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

        // Learning groups (WB-27)
        services.AddScoped<ICommandHandler<ApplyLearnerGroupEventCommand>, ApplyLearnerGroupEventCommandHandler>();
        services.AddScoped<IValidator<ApplyLearnerGroupEventCommand>, ApplyLearnerGroupEventCommandValidator>();
        services.AddScoped<IQueryHandler<GetGroupDashboardQuery, GroupDashboardDto>, GetGroupDashboardQueryHandler>();
        services.AddScoped<IValidator<GetGroupDashboardQuery>, GetGroupDashboardQueryValidator>();

        // Dashboard (WB-26). DashboardOptions and DashboardCalculator are registered by the host.
        services.AddScoped<IQueryHandler<GetLearnerDashboardQuery, LearnerDashboardDto>, GetLearnerDashboardQueryHandler>();
        services.AddScoped<IValidator<GetLearnerDashboardQuery>, GetLearnerDashboardQueryValidator>();

        // Vocabulary challenges (WB-29). ChallengeOptions is registered by the host.
        services.AddScoped<IChallengeConsentCleaner, ChallengeConsentCleaner>();
        services.AddScoped<ChallengeScoreProjector>();

        services.AddScoped<IQueryHandler<ListChallengesQuery, PagedResult<ChallengeDto>>, ListChallengesQueryHandler>();
        services.AddScoped<IValidator<ListChallengesQuery>, ListChallengesQueryValidator>();
        services.AddScoped<IQueryHandler<GetChallengeBoardQuery, ChallengeBoardDto>, GetChallengeBoardQueryHandler>();
        services.AddScoped<IValidator<GetChallengeBoardQuery>, GetChallengeBoardQueryValidator>();

        services.AddScoped<ICommandHandler<JoinChallengeCommand>, JoinChallengeCommandHandler>();
        services.AddScoped<IValidator<JoinChallengeCommand>, JoinChallengeCommandValidator>();
        services.AddScoped<ICommandHandler<LeaveChallengeCommand>, LeaveChallengeCommandHandler>();
        services.AddScoped<IValidator<LeaveChallengeCommand>, LeaveChallengeCommandValidator>();
        services.AddScoped<ICommandHandler<SetChildChallengeConsentCommand>, SetChildChallengeConsentCommandHandler>();
        services.AddScoped<IValidator<SetChildChallengeConsentCommand>, SetChildChallengeConsentCommandValidator>();
        services.AddScoped<ICommandHandler<AdvanceChallengeLifecycleCommand, int>, AdvanceChallengeLifecycleCommandHandler>();
        services.AddScoped<ICommandHandler<RecomputeChallengeScoresCommand, int>, RecomputeChallengeScoresCommandHandler>();
        services.AddScoped<ICommandHandler<ApplyLearnerProfileEventCommand>, ApplyLearnerProfileEventCommandHandler>();
        services.AddScoped<IValidator<ApplyLearnerProfileEventCommand>, ApplyLearnerProfileEventCommandValidator>();

        return services;
    }
}
