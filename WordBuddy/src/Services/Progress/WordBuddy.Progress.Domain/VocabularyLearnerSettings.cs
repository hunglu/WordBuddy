using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Domain;

/// <summary>
/// Per-user vocabulary settings, available to every user (D-4). Keyed by <see cref="UserId"/>.
/// An active supporter may set <see cref="SupporterNewWordCap"/>; it wins over the own cap (WB-24).
/// </summary>
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

    /// <summary>Daily new-word cap set by a supporter. <see langword="null"/> = none.</summary>
    public int? SupporterNewWordCap { get; private set; }

    /// <summary>The supporter who set <see cref="SupporterNewWordCap"/>.</summary>
    public Guid? SupporterCapSetBy { get; private set; }

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
            return Result.Failure(OutOfRange());
        }

        NewWordsPerDay = newWordsPerDay;
        return Result.Success();
    }

    /// <summary>A supporter sets (0–50) or clears (<see langword="null"/>) the supporter cap.</summary>
    public Result SetSupporterCap(int? cap, Guid supporterId)
    {
        if (cap is < MinNewWordsPerDay or > MaxNewWordsPerDay)
        {
            return Result.Failure(OutOfRange());
        }

        SupporterNewWordCap = cap;
        SupporterCapSetBy = cap is null ? null : supporterId;
        return Result.Success();
    }

    /// <summary>Clears the supporter cap when <paramref name="supporterId"/> set it (link revoked). Returns whether it changed.</summary>
    public bool ClearSupporterCapSetBy(Guid supporterId)
    {
        if (SupporterCapSetBy != supporterId)
        {
            return false;
        }

        SupporterNewWordCap = null;
        SupporterCapSetBy = null;
        return true;
    }

    private static Error OutOfRange() => Error.Validation(
        "VocabularySettings.OutOfRange",
        $"NewWordsPerDay must be between {MinNewWordsPerDay} and {MaxNewWordsPerDay}.");
}
