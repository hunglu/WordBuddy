using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Domain;

/// <summary>Per-user vocabulary settings, available to every user (D-4). Keyed by <see cref="UserId"/>.</summary>
public sealed class VocabularyLearnerSettings
{
    /// <summary>Lowest allowed <see cref="NewWordsPerDay"/>.</summary>
    public const int MinNewWordsPerDay = 0;

    /// <summary>Highest allowed <see cref="NewWordsPerDay"/>.</summary>
    public const int MaxNewWordsPerDay = 50;

    /// <summary>The user id (primary key).</summary>
    public Guid UserId { get; private set; }

    /// <summary>Own daily new-word cap. <see langword="null"/> = use the backlog rule (D-3).</summary>
    public int? NewWordsPerDay { get; private set; }

    private VocabularyLearnerSettings(Guid userId)
    {
        UserId = userId;
    }

    /// <summary>Creates settings for <paramref name="userId"/>.</summary>
    public static Result<VocabularyLearnerSettings> Create(Guid userId, int? newWordsPerDay)
    {
        VocabularyLearnerSettings settings = new(userId);
        Result result = settings.SetNewWordsPerDay(newWordsPerDay);
        return result.IsSuccess ? Result.Success(settings) : Result.Failure<VocabularyLearnerSettings>(result.Error);
    }

    /// <summary>Sets the cap. Must be 0–50 or <see langword="null"/>.</summary>
    public Result SetNewWordsPerDay(int? newWordsPerDay)
    {
        if (newWordsPerDay is < MinNewWordsPerDay or > MaxNewWordsPerDay)
        {
            return Result.Failure(Error.Validation(
                "VocabularySettings.OutOfRange",
                $"NewWordsPerDay must be between {MinNewWordsPerDay} and {MaxNewWordsPerDay}."));
        }

        NewWordsPerDay = newWordsPerDay;
        return Result.Success();
    }
}
