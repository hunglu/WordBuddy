using Microsoft.AspNetCore.Authorization;

namespace WordBuddy.Progress.Api.Authorization;

/// <summary>Challenge policy names.</summary>
public static class ChallengePolicies
{
    /// <summary>Caller holds the Guardian role: the JWT has <c>guardian = true</c> (issued by Identity). The handler checks the role again against the local profile.</summary>
    public const string GuardianOnly = "GuardianOnly";

    /// <summary>Adds the challenge policies.</summary>
    public static AuthorizationOptions AddChallengePolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(GuardianOnly, policy => policy.RequireAuthenticatedUser().RequireClaim("guardian", "true"));
        return options;
    }
}
