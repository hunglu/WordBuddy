namespace WordBuddy.API.Settings;

/// <summary>JWT token generation and validation settings.</summary>
public sealed class JwtSettings
{
    /// <summary>Gets the signing secret. Must be at least 32 characters.</summary>
    public string Secret { get; init; } = string.Empty;

    /// <summary>Gets the token issuer claim value.</summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>Gets the token lifetime in minutes.</summary>
    public int ExpiryMinutes { get; init; } = 60;
}
