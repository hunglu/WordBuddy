using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace WordBuddy.Progress.IntegrationTests;

/// <summary>Issues JWTs shaped exactly like Identity's own <c>JwtTokenGenerator</c> (same claim
/// names, including <c>sub</c> — the only claim Progress's <c>ClaimsPrincipalExtensions.GetUserId</c>
/// reads), signed with the same secret/issuer <see cref="ProgressApiFactory"/> configures the API
/// host to validate against.</summary>
public static class TestJwtTokenFactory
{
    public static string CreateToken(Guid userId)
    {
        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        ];

        SymmetricSecurityKey signingKey = new(Encoding.UTF8.GetBytes(ProgressApiFactory.JwtSecret));
        SigningCredentials credentials = new(signingKey, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new(
            issuer: ProgressApiFactory.JwtIssuer,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
