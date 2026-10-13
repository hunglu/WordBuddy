using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Lessons.Commands.DetachLessonSense;

public sealed class DetachLessonSenseCommandHandler : ICommandHandler<DetachLessonSenseCommand>
{
    private readonly ILessonRepository _lessonRepository;
    private readonly ILogger<DetachLessonSenseCommandHandler> _logger;

    public DetachLessonSenseCommandHandler(
        ILessonRepository lessonRepository,
        ILogger<DetachLessonSenseCommandHandler> logger)
    {
        _lessonRepository = lessonRepository;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(DetachLessonSenseCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("DetachLessonSenseCommand started: LessonId={LessonId}, SenseId={SenseId}", command.LessonId, command.SenseId);

        Result<Lesson> lessonResult = await _lessonRepository.GetByIdForUpdateAsync(command.LessonId, ct);
        if (lessonResult.IsFailure)
        {
            _logger.LogWarning(
                "DetachLessonSenseCommand lesson lookup failed: {ErrorCode} — {ErrorDescription}",
                lessonResult.Error.Code, lessonResult.Error.Description);
            return Result.Failure(lessonResult.Error);
        }

        Result detachResult = lessonResult.Value.DetachSense(command.SenseId);
        if (detachResult.IsFailure)
        {
            _logger.LogWarning(
                "DetachLessonSenseCommand rejected by lesson: {ErrorCode} — {ErrorDescription}",
                detachResult.Error.Code, detachResult.Error.Description);
            return detachResult;
        }

        Result saveResult = await _lessonRepository.SaveChangesAsync(ct);
        if (saveResult.IsFailure)
        {
            _logger.LogWarning(
                "DetachLessonSenseCommand failed to persist: {ErrorCode} — {ErrorDescription}",
                saveResult.Error.Code, saveResult.Error.Description);
            return saveResult;
        }

        _logger.LogInformation("DetachLessonSenseCommand succeeded: LessonId={LessonId}", command.LessonId);
        return Result.Success();
    }
}
