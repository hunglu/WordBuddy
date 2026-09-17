using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;

namespace WordBuddy.Progress.Application.Features.Progress.Queries.GetUserProgress;

public sealed record GetUserProgressQuery(Guid UserId) : IQuery<IReadOnlyList<LearnerProgressDto>>;
