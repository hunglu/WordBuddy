namespace WordBuddy.Identity.Api.Extensions;

/// <summary>Named authorization policies of the Identity API.</summary>
internal static class AuthorizationPolicies
{
    /// <summary>Caller has the <c>is_admin = true</c> claim.</summary>
    public const string AdminOnly = "AdminOnly";
}
