using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace WordBuddy.Content.IntegrationTests;

/// <summary>Issues JWTs shaped exactly like Identity's own <c>JwtTokenGenerator</c> (same claim
/// names: <c>sub</c>, <c>age_group</c>, <c>is_admin</c>), signed with the same secret/issuer
/// <see cref="ContentApiFactory"/> configures the API host to validate against.</summary>
public static class TestJwtTokenFactory
{
    public static string CreateToken(Guid userId, string ageGroup, bool isAdmin)
    {
        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("age_group", ageGroup),
            new("is_admin", isAdmin ? "true" : "false"),
        ];

        SymmetricSecurityKey signingKey = new(Encoding.UTF8.GetBytes(ContentApiFactory.JwtSecret));
        SigningCredentials credentials = new(signingKey, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new(
            issuer: ContentApiFactory.JwtIssuer,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
