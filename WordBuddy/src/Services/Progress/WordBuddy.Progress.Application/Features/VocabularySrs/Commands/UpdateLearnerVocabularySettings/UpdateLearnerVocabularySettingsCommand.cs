using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;

namespace WordBuddy.Progress.Application.Features.VocabularySrs.Commands.UpdateLearnerVocabularySettings;

/// <summary>An active supporter sets (0–50) or clears (<see langword="null"/>) the learner new-word cap.
/// Any active supporter may do this (Q7); the policy <c>CanSupportLearner</c> checks the link.</summary>
public sealed record UpdateLearnerVocabularySettingsCommand(
    Guid SupporterId,
    Guid LearnerId,
    int? SupporterNewWordCap) : ICommand<LearnerVocabularySettingsDto>;
