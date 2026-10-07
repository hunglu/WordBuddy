using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.Api.Extensions;

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

    /// <summary>
    /// Extracts the age group from the JWT's <c>age_group</c> claim (same claim Content reads).
    /// Missing or unknown → <see cref="AgeGroup.Child"/>, the safer side: it only makes grading
    /// thresholds more lenient (D-7).
    /// </summary>
    public static AgeGroup GetAgeGroup(this ClaimsPrincipal user)
    {
        string? ageGroup = user.FindFirstValue("age_group");

        return ageGroup is not null && Enum.TryParse(ageGroup, ignoreCase: true, out AgeGroup value) && Enum.IsDefined(value)
            ? value
            : AgeGroup.Child;
    }
}
