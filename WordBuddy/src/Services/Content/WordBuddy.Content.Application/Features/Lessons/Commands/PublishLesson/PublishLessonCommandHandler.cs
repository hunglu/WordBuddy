using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Lessons.Commands.PublishLesson;

public sealed class PublishLessonCommandHandler : ICommandHandler<PublishLessonCommand>
{
    private readonly ILessonRepository _lessonRepository;
    private readonly ILogger<PublishLessonCommandHandler> _logger;

    public PublishLessonCommandHandler(
        ILessonRepository lessonRepository,
        ILogger<PublishLessonCommandHandler> logger)
    {
        _lessonRepository = lessonRepository;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(PublishLessonCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("PublishLessonCommand started: LessonId={LessonId}", command.LessonId);

        Result<Lesson> lessonResult = await _lessonRepository.GetByIdForUpdateAsync(command.LessonId, ct);
        if (lessonResult.IsFailure)
        {
            _logger.LogWarning(
                "PublishLessonCommand lesson lookup failed: {ErrorCode} — {ErrorDescription}",
                lessonResult.Error.Code, lessonResult.Error.Description);
            return Result.Failure(lessonResult.Error);
        }

        Result changeResult = lessonResult.Value.Publish();
        if (changeResult.IsFailure)
        {
            _logger.LogWarning(
                "PublishLessonCommand rejected by lesson: {ErrorCode} — {ErrorDescription}",
                changeResult.Error.Code, changeResult.Error.Description);
            return changeResult;
        }

        Result saveResult = await _lessonRepository.SaveChangesAsync(ct);
        if (saveResult.IsFailure)
        {
            _logger.LogWarning(
                "PublishLessonCommand failed to persist: {ErrorCode} — {ErrorDescription}",
                saveResult.Error.Code, saveResult.Error.Description);
            return saveResult;
        }

        _logger.LogInformation("PublishLessonCommand succeeded: LessonId={LessonId}", command.LessonId);
        return Result.Success();
    }
}
