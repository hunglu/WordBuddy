using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WordBuddy.Identity.Api.Extensions;

internal static class ClaimsPrincipalExtensions
{
    /// <summary>Whether the caller carries the <c>is_admin = true</c> claim.</summary>
    public static bool IsAdmin(this ClaimsPrincipal user) => user.HasClaim("is_admin", "true");

    /// <summary>
    /// Extracts the authenticated user id from the JWT <c>sub</c> claim. Checks both
    /// <see cref="ClaimTypes.NameIdentifier"/> (the default inbound mapping target for <c>sub</c>)
    /// and the raw <c>sub</c> claim type.
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
