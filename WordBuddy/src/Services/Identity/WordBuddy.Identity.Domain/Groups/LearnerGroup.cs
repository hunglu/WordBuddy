using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Domain.Groups;

/// <summary>A named group of learners owned by one adult supporter. Soft-deleted, never removed.</summary>
public sealed class LearnerGroup : Entity
{
    /// <summary>Minimum name length.</summary>
    public const int NameMinLength = 3;

    /// <summary>Maximum name length.</summary>
    public const int NameMaxLength = 60;

    /// <summary>Gets the owner (an adult supporter).</summary>
    public Guid OwnerId { get; private set; }

    /// <summary>Gets the group name. Never log it.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets when the group was created, UTC.</summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Gets when the group last changed, UTC.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>Gets when the group was deleted, UTC. Null while the group exists.</summary>
    public DateTime? DeletedAtUtc { get; private set; }

    /// <summary>Gets a value indicating whether the group is deleted.</summary>
    public bool IsDeleted => DeletedAtUtc.HasValue;

    private LearnerGroup(Guid id) : base(id)
    {
    }

    /// <summary>Creates a group. The name is trimmed and must be 3 to 60 characters.</summary>
    public static Result<LearnerGroup> Create(Guid id, Guid ownerId, string? name, DateTime nowUtc)
    {
        Result<string> valid = ValidateName(name);
        if (valid.IsFailure)
        {
            return Result.Failure<LearnerGroup>(valid.Error);
        }

        return Result.Success(new LearnerGroup(id)
        {
            OwnerId = ownerId,
            Name = valid.Value,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
        });
    }

    /// <summary>Renames the group.</summary>
    public Result Rename(string? name, DateTime nowUtc)
    {
        if (IsDeleted)
        {
            return Result.Failure(LearnerGroupErrors.NotFound);
        }

        Result<string> valid = ValidateName(name);
        if (valid.IsFailure)
        {
            return Result.Failure(valid.Error);
        }

        Name = valid.Value;
        UpdatedAtUtc = nowUtc;
        return Result.Success();
    }

    /// <summary>Soft-deletes the group. The caller removes the members.</summary>
    public Result Delete(DateTime nowUtc)
    {
        if (IsDeleted)
        {
            return Result.Failure(LearnerGroupErrors.NotFound);
        }

        DeletedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
        return Result.Success();
    }

    private static Result<string> ValidateName(string? name)
    {
        string trimmed = name?.Trim() ?? string.Empty;
        return trimmed.Length is < NameMinLength or > NameMaxLength
            ? Result.Failure<string>(LearnerGroupErrors.NameLength)
            : Result.Success(trimmed);
    }
}
