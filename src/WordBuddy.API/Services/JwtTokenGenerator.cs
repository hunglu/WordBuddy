using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WordBuddy.API.Settings;
using WordBuddy.Application.DTOs;

namespace WordBuddy.API.Services;

/// <summary>Generates signed JWT access tokens for authenticated users.</summary>
public sealed class JwtTokenGenerator
{
    private readonly JwtSettings _settings;

    /// <summary>Initializes a new <see cref="JwtTokenGenerator"/>.</summary>
    public JwtTokenGenerator(JwtSettings settings) => _settings = settings;

    /// <summary>Creates a signed JWT for <paramref name="user"/> and wraps it in an <see cref="AuthTokenDto"/>.</summary>
    public AuthTokenDto Generate(UserDto user)
    {
        DateTime expiresAt = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(_settings.Secret));
        SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);

        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        ];

        JwtSecurityToken jwtToken = new(
            issuer: _settings.Issuer,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        string tokenString = new JwtSecurityTokenHandler().WriteToken(jwtToken);
        return new AuthTokenDto(tokenString, expiresAt, user.Id, user.DisplayName, user.Email);
    }
}
