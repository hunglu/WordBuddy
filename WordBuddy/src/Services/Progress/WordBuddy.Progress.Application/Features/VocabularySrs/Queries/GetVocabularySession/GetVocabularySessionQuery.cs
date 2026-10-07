using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;

namespace WordBuddy.Progress.Application.Features.VocabularySrs.Queries.GetVocabularySession;

/// <summary>Builds today's session for <paramref name="UserId"/> (from the JWT).
/// <paramref name="ClientCurrentDateTime"/> is the raw <c>X-Client-CurrentDateTime</c> header, or
/// <see langword="null"/> when missing (→ UTC day).</summary>
public sealed record GetVocabularySessionQuery(Guid UserId, string? ClientCurrentDateTime) : IQuery<VocabularySessionDto>;
