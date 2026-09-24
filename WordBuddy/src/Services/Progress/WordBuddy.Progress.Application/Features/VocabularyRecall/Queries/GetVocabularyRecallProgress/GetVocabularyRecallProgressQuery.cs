using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;

namespace WordBuddy.Progress.Application.Features.VocabularyRecall.Queries.GetVocabularyRecallProgress;

/// <summary><paramref name="UserId"/> comes from the authenticated caller's JWT.</summary>
public sealed record GetVocabularyRecallProgressQuery(Guid UserId) : IQuery<VocabularyRecallProgressDto>;
