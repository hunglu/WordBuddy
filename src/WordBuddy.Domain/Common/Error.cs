namespace WordBuddy.Domain.Common;

/// <summary>Typed domain error carrying a machine-readable <see cref="Code"/> and a human-readable <see cref="Description"/>.</summary>
public sealed record Error(string Code, string Description)
{
    /// <summary>Sentinel representing the absence of an error; present on every successful <see cref="Result{T}"/>.</summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>Creates a not-found error.</summary>
    public static Error NotFound(string code, string description) => new(code, description);

    /// <summary>Creates a validation error.</summary>
    public static Error Validation(string code, string description) => new(code, description);

    /// <summary>Creates a conflict error (e.g. duplicate resource).</summary>
    public static Error Conflict(string code, string description) => new(code, description);

    /// <summary>Creates a generic failure error.</summary>
    public static Error Failure(string code, string description) => new(code, description);
}
