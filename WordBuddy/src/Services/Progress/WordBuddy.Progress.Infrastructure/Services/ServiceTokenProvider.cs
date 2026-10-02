using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace WordBuddy.Progress.Infrastructure.Services;

/// <summary>Mints the short-lived service JWT Progress presents to Content's internal endpoints:
/// HS256 over the shared <c>Jwt:Secret</c>, <c>iss = Jwt:Issuer</c>, <c>sub = wordbuddy-progress</c>,
/// <c>wb_service = progress</c>, valid for <see cref="Lifetime"/>. Cached and reused until
/// <see cref="RefreshBefore"/> before expiry. The token is never logged.</summary>
public sealed class ServiceTokenProvider
{
    /// <summary>Subject claim value identifying the Progress service.</summary>
    public const string Subject = "wordbuddy-progress";

    /// <summary>Claim Content's <c>InternalService</c> policy requires.</summary>
    public const string ServiceClaimType = "wb_service";

    /// <summary>Value of <see cref="ServiceClaimType"/> for Progress.</summary>
    public const string ServiceClaimValue = "progress";

    /// <summary>How long a minted token is valid.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    /// <summary>A cached token is replaced once it is this close to expiry.</summary>
    public static readonly TimeSpan RefreshBefore = TimeSpan.FromMinutes(1);

    private readonly SigningCredentials _credentials;
    private readonly string _issuer;
    private readonly TimeProvider _timeProvider;
    private readonly JsonWebTokenHandler _handler = new();
    private readonly Lock _lock = new();

    private string? _cachedToken;
    private DateTimeOffset _cachedExpiresAt;

    public ServiceTokenProvider(string secret, string issuer, TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);

        _credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)), SecurityAlgorithms.HmacSha256);
        _issuer = issuer;
        _timeProvider = timeProvider;
    }

    /// <summary>Returns a valid service token, minting a new one when none is cached or the cached
    /// one expires within <see cref="RefreshBefore"/>.</summary>
    public string GetToken()
    {
        lock (_lock)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            if (_cachedToken is not null && now < _cachedExpiresAt - RefreshBefore)
            {
                return _cachedToken;
            }

            DateTimeOffset expiresAt = now + Lifetime;
            SecurityTokenDescriptor descriptor = new()
            {
                Issuer = _issuer,
                Subject = new ClaimsIdentity(
                [
                    new Claim(JwtRegisteredClaimNames.Sub, Subject),
                    new Claim(ServiceClaimType, ServiceClaimValue),
                ]),
                IssuedAt = now.UtcDateTime,
                NotBefore = now.UtcDateTime,
                Expires = expiresAt.UtcDateTime,
                SigningCredentials = _credentials,
            };

            _cachedToken = _handler.CreateToken(descriptor);
            _cachedExpiresAt = expiresAt;
            return _cachedToken;
        }
    }
}
