using FluentValidation;
using FluentValidation.Results;
using WordBuddy.Application.DTOs;
using WordBuddy.Application.Interfaces;
using WordBuddy.Domain.Common;
using WordBuddy.Domain.Entities;

namespace WordBuddy.Application.Features.Progress.Queries.GetUserProgress;

/// <summary>Returns all lesson progress records for a user, mapped to DTOs.</summary>
public sealed class GetUserProgressQueryHandler
{
    private readonly ILearnerProgressRepository _progressRepository;
    private readonly IValidator<GetUserProgressQuery> _validator;

    /// <summary>Initializes a new <see cref="GetUserProgressQueryHandler"/>.</summary>
    public GetUserProgressQueryHandler(
        ILearnerProgressRepository progressRepository,
        IValidator<GetUserProgressQuery> validator)
    {
        _progressRepository = progressRepository;
        _validator = validator;
    }

    /// <summary>Handles the query and returns all progress records for the specified user.</summary>
    public async Task<Result<List<LearnerProgressDto>>> HandleAsync(GetUserProgressQuery query, CancellationToken ct = default)
    {
        ValidationResult validation = await _validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
            return Result<List<LearnerProgressDto>>.Failure(
                Error.Validation("GetUserProgress.Validation", validation.ToString()));

        Result<IReadOnlyList<LearnerProgress>> progressResult =
            await _progressRepository.GetByUserIdAsync(query.UserId, ct);

        if (progressResult.IsFailure)
            return Result<List<LearnerProgressDto>>.Failure(progressResult.Error);

        List<LearnerProgressDto> dtos = progressResult.Value
            .Select(p => new LearnerProgressDto(
                p.Id, p.UserId, p.LessonId, p.IsCompleted,
                p.ScorePercent, p.LastAccessedAt, p.CompletedAt))
            .ToList();

        return Result<List<LearnerProgressDto>>.Success(dtos);
    }
}
