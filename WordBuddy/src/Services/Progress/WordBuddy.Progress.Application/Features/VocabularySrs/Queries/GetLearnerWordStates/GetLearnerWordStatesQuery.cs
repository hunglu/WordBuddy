using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;

namespace WordBuddy.Progress.Application.Features.VocabularySrs.Queries.GetLearnerWordStates;

/// <summary>The caller's own active word states. <paramref name="UserId"/> comes from the JWT.</summary>
public sealed record GetLearnerWordStatesQuery(Guid UserId) : IQuery<IReadOnlyList<LearnerWordStateDto>>;
