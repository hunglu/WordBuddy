using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.VocabularySrs.Commands.CreateVocabularyExercise;

/// <summary>
/// Picks the exercise type, builds the options from the session's other words, stores the expected
/// answer and returns the prompt without the answer. Senses come from Content with the caller's
/// identity, so a child only gets child-visible words as prompt or distractor. Logs ids only.
/// </summary>
public sealed class CreateVocabularyExerciseCommandHandler : ICommandHandler<CreateVocabularyExerciseCommand, VocabularyExerciseDto>
{
    /// <summary>After this many correct answers a word is done for the session.</summary>
    private const int MaxCorrectAnswersPerSession = 2;

    private const int OptionKeyLength = 8;

    private readonly IVocabularySessionIssueRepository _sessions;
    private readonly ILearnerWordStateRepository _states;
    private readonly IReviewLogRepository _reviewLogs;
    private readonly IVocabularyExerciseRepository _exercises;
    private readonly IContentSenseClient _contentSenses;
    private readonly TimeProvider _timeProvider;
    private readonly IValidator<CreateVocabularyExerciseCommand> _validator;
    private readonly ILogger<CreateVocabularyExerciseCommandHandler> _logger;

    public CreateVocabularyExerciseCommandHandler(
        IVocabularySessionIssueRepository sessions,
        ILearnerWordStateRepository states,
        IReviewLogRepository reviewLogs,
        IVocabularyExerciseRepository exercises,
        IContentSenseClient contentSenses,
        TimeProvider timeProvider,
        IValidator<CreateVocabularyExerciseCommand> validator,
        ILogger<CreateVocabularyExerciseCommandHandler> logger)
    {
        _sessions = sessions;
        _states = states;
        _reviewLogs = reviewLogs;
        _exercises = exercises;
        _contentSenses = contentSenses;
        _timeProvider = timeProvider;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<VocabularyExerciseDto>> HandleAsync(CreateVocabularyExerciseCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "CreateVocabularyExerciseCommand started: UserId={UserId}, SessionId={SessionId}, SenseId={SenseId}",
            command.UserId, command.SessionId, command.SenseId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("CreateVocabularyExerciseCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure<VocabularyExerciseDto>(Error.Validation("CreateVocabularyExercise.Validation", validation.ToString()));
        }

        DateTime nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        Result<VocabularySessionIssue> open = await _sessions.GetOpenAsync(command.UserId, nowUtc, ct);
        if (open.IsFailure && open.Error.Type != ErrorType.NotFound)
        {
            return Result.Failure<VocabularyExerciseDto>(open.Error);
        }

        if (open.IsFailure || open.Value.SessionId != command.SessionId)
        {
            _logger.LogWarning(
                "CreateVocabularyExerciseCommand session not open: UserId={UserId}, SessionId={SessionId}",
                command.UserId, command.SessionId);
            return Result.Failure<VocabularyExerciseDto>(
                Error.NotFound("Exercise.SessionNotOpen", "The session is closed, expired or not yours."));
        }

        VocabularySessionIssue session = open.Value;
        if (session.Items.All(i => i.SenseId != command.SenseId))
        {
            return Result.Failure<VocabularyExerciseDto>(
                Error.NotFound("Exercise.SenseNotInSession", "The word is not part of this session."));
        }

        Result<LearnerWordState> stateResult = await _states.GetTrackedAsync(command.UserId, command.SenseId, ct);
        if (stateResult.IsFailure && stateResult.Error.Type != ErrorType.NotFound)
        {
            return Result.Failure<VocabularyExerciseDto>(stateResult.Error);
        }

        if (stateResult.IsFailure || !stateResult.Value.IsActive)
        {
            return Result.Failure<VocabularyExerciseDto>(
                Error.NotFound("Review.WordNotInList", "The word is not in the learner's list."));
        }

        Result<int> correctResult = await _reviewLogs.CountCorrectAsync(command.UserId, command.SessionId, command.SenseId, ct);
        if (correctResult.IsFailure)
        {
            return Result.Failure<VocabularyExerciseDto>(correctResult.Error);
        }

        if (correctResult.Value >= MaxCorrectAnswersPerSession)
        {
            return Result.Failure<VocabularyExerciseDto>(
                Error.Conflict("Exercise.SenseCompleted", "The word is already answered correctly in this session."));
        }

        Result<IReadOnlyList<ContentSenseDto>> sensesResult = await _contentSenses.GetSessionSensesAsync(
            session.SessionId, session.Items.Select(i => i.SenseId).ToList(), session.ExpiresAtUtc, ct);
        if (sensesResult.IsFailure)
        {
            _logger.LogWarning(
                "CreateVocabularyExerciseCommand could not load senses: SessionId={SessionId}, ErrorCode={ErrorCode}",
                command.SessionId, sensesResult.Error.Code);
            return Result.Failure<VocabularyExerciseDto>(sensesResult.Error);
        }

        IReadOnlyList<ContentSenseDto> senses = sensesResult.Value;
        ContentSenseDto? target = senses.FirstOrDefault(s => s.SenseId == command.SenseId);
        if (target is null)
        {
            _logger.LogWarning(
                "CreateVocabularyExerciseCommand sense unavailable: SessionId={SessionId}, SenseId={SenseId}",
                command.SessionId, command.SenseId);
            return Result.Failure<VocabularyExerciseDto>(
                Error.NotFound("Exercise.SenseUnavailable", "The word is not available right now."));
        }

        WordStatus status = stateResult.Value.Status;
        ExerciseLevel level = ExerciseSelector.LevelFor(status, correctResult.Value);
        ExerciseChoice choice = ExerciseSelector.Select(
            status, level, HasValue(target.ImageUrl), HasValue(target.AudioUrl), senses.Count);

        List<ContentSenseDto> distractors = [];
        if (choice.ExerciseType != ExerciseType.Typing)
        {
            distractors = PickDistractors(target, senses);
            if (distractors.Count < ExerciseSelector.ChoiceOptionCount - 1)
            {
                choice = ExerciseSelector.TypingChoice;
            }
        }

        Guid exerciseId = Guid.NewGuid();
        VocabularyExercise exercise;
        List<ExerciseOptionDto> optionDtos = [];

        if (choice.ExerciseType == ExerciseType.Typing)
        {
            exercise = VocabularyExercise.Create(
                exerciseId, command.UserId, command.SessionId, command.SenseId, choice.ExerciseType, choice.Skill,
                AnswerChecker.Normalize(target.Word), target.Word, new Dictionary<string, Guid>(), nowUtc);
        }
        else
        {
            List<ContentSenseDto> shuffled = Shuffle([target, .. distractors]);
            Dictionary<string, Guid> optionMap = [];
            string correctKey = string.Empty;
            foreach (ContentSenseDto sense in shuffled)
            {
                string key = NewOptionKey(optionMap);
                optionMap[key] = sense.SenseId;
                optionDtos.Add(new ExerciseOptionDto(key, sense.Word));
                if (sense.SenseId == target.SenseId)
                {
                    correctKey = key;
                }
            }

            exercise = VocabularyExercise.Create(
                exerciseId, command.UserId, command.SessionId, command.SenseId, choice.ExerciseType, choice.Skill,
                correctKey, target.Word, optionMap, nowUtc);
        }

        Result addResult = await _exercises.AddAsync(exercise, ct);
        if (addResult.IsFailure)
        {
            return Result.Failure<VocabularyExerciseDto>(addResult.Error);
        }

        Result saveResult = await _states.SaveChangesAsync(ct);
        if (saveResult.IsFailure)
        {
            _logger.LogWarning(
                "CreateVocabularyExerciseCommand failed to save: {ErrorCode} — {ErrorDescription}",
                saveResult.Error.Code, saveResult.Error.Description);
            return Result.Failure<VocabularyExerciseDto>(saveResult.Error);
        }

        ExercisePromptDto prompt = new(
            target.Definition,
            choice.ExerciseType == ExerciseType.PictureChoice ? target.ImageUrl : null,
            choice.ExerciseType == ExerciseType.ListeningChoice ? target.AudioUrl : null,
            target.PersonalContext,
            choice.ExerciseType == ExerciseType.Typing ? FirstLetter(target.Word) : null);

        _logger.LogInformation(
            "CreateVocabularyExerciseCommand succeeded: ExerciseId={ExerciseId}, SenseId={SenseId}, ExerciseType={ExerciseType}",
            exerciseId, command.SenseId, choice.ExerciseType);

        return Result.Success(new VocabularyExerciseDto(exerciseId, choice.ExerciseType, choice.Skill, prompt, optionDtos));
    }

    private static bool HasValue(string? value) => !string.IsNullOrWhiteSpace(value);

    private static string? FirstLetter(string word)
    {
        string trimmed = word.Trim();
        return trimmed.Length == 0 ? null : trimmed[..1];
    }

    /// <summary>Up to 3 other session senses with a different word (compared case-insensitive), one per word.</summary>
    private static List<ContentSenseDto> PickDistractors(ContentSenseDto target, IReadOnlyList<ContentSenseDto> senses)
    {
        HashSet<string> seenWords = new(StringComparer.OrdinalIgnoreCase) { target.Word.Trim() };
        List<ContentSenseDto> candidates = [];
        foreach (ContentSenseDto sense in Shuffle(senses.Where(s => s.SenseId != target.SenseId)))
        {
            if (seenWords.Add(sense.Word.Trim()))
            {
                candidates.Add(sense);
            }
        }

        return candidates.Take(ExerciseSelector.ChoiceOptionCount - 1).ToList();
    }

    private static string NewOptionKey(Dictionary<string, Guid> existing)
    {
        string key;
        do
        {
            key = Guid.NewGuid().ToString("N")[..OptionKeyLength];
        }
        while (existing.ContainsKey(key));

        return key;
    }

    private static List<T> Shuffle<T>(IEnumerable<T> items)
    {
        List<T> copy = items.ToList();
        for (int i = copy.Count - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (copy[i], copy[j]) = (copy[j], copy[i]);
        }

        return copy;
    }
}
