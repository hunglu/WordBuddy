using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace WordBuddy.Progress.IntegrationTests;

/// <summary>Issues JWTs shaped exactly like Identity's own <c>JwtTokenGenerator</c> (same claim
/// names: <c>sub</c> and the optional <c>age_group</c>, the claims Progress's
/// <c>ClaimsPrincipalExtensions</c> read), signed with the same secret/issuer <see cref="ProgressApiFactory"/> configures the API
/// host to validate against.</summary>
public static class TestJwtTokenFactory
{
    public static string CreateToken(Guid userId, string? ageGroup = null)
    {
        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        ];

        if (ageGroup is not null)
        {
            claims.Add(new Claim("age_group", ageGroup));
        }

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
