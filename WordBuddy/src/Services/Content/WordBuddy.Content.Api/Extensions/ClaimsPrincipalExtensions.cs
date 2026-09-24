using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Api.Extensions;

internal static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Extracts the authenticated user's id from the JWT's <c>sub</c> claim. Checks both
    /// <see cref="ClaimTypes.NameIdentifier"/> (the default inbound mapping target for <c>sub</c>
    /// applied by <c>JwtSecurityTokenHandler</c>) and the raw <c>sub</c> claim type, in case
    /// inbound claim mapping is ever disabled.
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        string? subject = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (subject is null || !Guid.TryParse(subject, out Guid userId))
        {
            throw new InvalidOperationException("The authenticated request has no valid 'sub' claim.");
        }

        return userId;
    }

    /// <summary>Extracts the authenticated user's age group from the JWT's <c>age_group</c> claim.</summary>
    public static AgeGroup GetAgeGroup(this ClaimsPrincipal user)
    {
        string? ageGroup = user.FindFirstValue("age_group");

        if (ageGroup is null || !Enum.TryParse(ageGroup, out AgeGroup value))
        {
            throw new InvalidOperationException("The authenticated request has no valid 'age_group' claim.");
        }

        return value;
    }

    /// <summary>Whether the authenticated user has administrative privileges, per the JWT's <c>is_admin</c> claim.</summary>
    public static bool IsAdmin(this ClaimsPrincipal user) =>
        string.Equals(user.FindFirstValue("is_admin"), "true", StringComparison.OrdinalIgnoreCase);
}
