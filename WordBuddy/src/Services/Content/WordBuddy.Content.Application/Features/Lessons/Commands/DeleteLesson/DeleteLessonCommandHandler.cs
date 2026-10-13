using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Lessons.Commands.DeleteLesson;

public sealed class DeleteLessonCommandHandler : ICommandHandler<DeleteLessonCommand>
{
    private readonly ILessonRepository _lessonRepository;
    private readonly ILogger<DeleteLessonCommandHandler> _logger;

    public DeleteLessonCommandHandler(
        ILessonRepository lessonRepository,
        ILogger<DeleteLessonCommandHandler> logger)
    {
        _lessonRepository = lessonRepository;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(DeleteLessonCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("DeleteLessonCommand started: LessonId={LessonId}", command.LessonId);

        Result<Lesson> lessonResult = await _lessonRepository.GetByIdForUpdateAsync(command.LessonId, ct);
        if (lessonResult.IsFailure)
        {
            _logger.LogWarning(
                "DeleteLessonCommand lesson lookup failed: {ErrorCode} — {ErrorDescription}",
                lessonResult.Error.Code, lessonResult.Error.Description);
            return Result.Failure(lessonResult.Error);
        }

        _lessonRepository.Remove(lessonResult.Value);

        Result saveResult = await _lessonRepository.SaveChangesAsync(ct);
        if (saveResult.IsFailure)
        {
            _logger.LogWarning(
                "DeleteLessonCommand failed to persist: {ErrorCode} — {ErrorDescription}",
                saveResult.Error.Code, saveResult.Error.Description);
            return saveResult;
        }

        _logger.LogInformation("DeleteLessonCommand succeeded: LessonId={LessonId}", command.LessonId);
        return Result.Success();
    }
}
