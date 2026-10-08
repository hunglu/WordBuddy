using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;

namespace WordBuddy.Progress.Application.Features.VocabularySrs.Queries.GetLearnerVocabularySettings;

/// <summary>A learner settings, read by an active supporter (policy <c>CanSupportLearner</c> checks the link).</summary>
public sealed record GetLearnerVocabularySettingsQuery(Guid SupporterId, Guid LearnerId) : IQuery<LearnerVocabularySettingsDto>;
