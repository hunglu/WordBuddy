using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;

namespace WordBuddy.Progress.Application.Features.VocabularySrs.Commands.UpdateVocabularySettings;

/// <summary>Sets the caller's own daily new-word cap (0–50), or <see langword="null"/> for the backlog
/// rule. Allowed for every user, child included (D-4).</summary>
public sealed record UpdateVocabularySettingsCommand(Guid UserId, int? NewWordsPerDay) : ICommand<VocabularySettingsDto>;
