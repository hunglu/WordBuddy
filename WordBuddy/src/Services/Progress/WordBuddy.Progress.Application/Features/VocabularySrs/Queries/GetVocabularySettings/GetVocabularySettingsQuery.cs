using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;

namespace WordBuddy.Progress.Application.Features.VocabularySrs.Queries.GetVocabularySettings;

/// <summary>The caller's vocabulary settings (any user, D-4).</summary>
public sealed record GetVocabularySettingsQuery(Guid UserId) : IQuery<VocabularySettingsDto>;
