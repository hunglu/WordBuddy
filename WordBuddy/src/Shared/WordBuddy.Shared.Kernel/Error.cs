namespace WordBuddy.Shared.Kernel;

/// <summary>A typed application error. Never thrown — carried inside a <see cref="Result"/> or <see cref="Result{T}"/>.</summary>
public sealed record Error(string Code, string Description, ErrorType Type)
{
    /// <summary>Creates a validation error (maps to HTTP 400).</summary>
    public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);

    /// <summary>Creates a not-found error (maps to HTTP 404).</summary>
    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);

    /// <summary>Creates a conflict error (maps to HTTP 409).</summary>
    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);

    /// <summary>Creates a generic failure error (maps to HTTP 400).</summary>
    public static Error Failure(string code, string description) => new(code, description, ErrorType.Failure);

    /// <summary>The absence of an error. Used internally by <see cref="Result"/>; never inspect this from calling code.</summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);
}

/// <summary>Broad category of an <see cref="Error"/>, used to map it to an HTTP status code at the API boundary.</summary>
public enum ErrorType
{
    None,
    Failure,
    Validation,
    NotFound,
    Conflict
}
