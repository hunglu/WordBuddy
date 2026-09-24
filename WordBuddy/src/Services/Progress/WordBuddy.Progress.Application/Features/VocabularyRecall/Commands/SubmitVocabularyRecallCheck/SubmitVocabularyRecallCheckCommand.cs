using WordBuddy.Progress.Application.Abstractions;

namespace WordBuddy.Progress.Application.Features.VocabularyRecall.Commands.SubmitVocabularyRecallCheck;

/// <summary><paramref name="UserId"/> comes from the authenticated caller's JWT — never trust a
/// client-supplied user id. Each result's <c>Word</c> is a denormalized copy of the word text the
/// frontend already fetched from Content when it started the check session — see the plan's
/// Data/migration notes for why this avoids a live cross-service lookup.</summary>
public sealed record SubmitVocabularyRecallCheckCommand(
    Guid UserId,
    IReadOnlyList<VocabularyRecallResultItem> Results) : ICommand;

/// <summary>One word's outcome within a recall-check submission.</summary>
public sealed record VocabularyRecallResultItem(Guid VocabularyWordId, string Word, bool Known);
