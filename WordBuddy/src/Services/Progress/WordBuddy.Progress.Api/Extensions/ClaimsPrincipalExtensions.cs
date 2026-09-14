using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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
}
