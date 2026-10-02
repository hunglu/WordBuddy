using WordBuddy.Progress.Application.Abstractions;

namespace WordBuddy.Progress.Application.Features.VocabularyRecall.Commands.RemapVocabularyWordIds;

/// <summary>Content merged word <paramref name="Remaps"/>[i].OldId into NewId; every recall stat
/// that tracks an old id must follow. Sent by Content, not by a learner — there is no user scope.</summary>
public sealed record RemapVocabularyWordIdsCommand(IReadOnlyList<VocabularyWordIdRemap> Remaps) : ICommand;

/// <summary>One merged Content word id.</summary>
public sealed record VocabularyWordIdRemap(Guid OldId, Guid NewId);
