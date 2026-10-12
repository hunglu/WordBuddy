using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Domain.Challenges;

/// <summary>Typed errors of the challenge feature. Descriptions hold no personal data.</summary>
/// <remarks>The API maps the codes in <see cref="ForbiddenCodes"/> to 403.</remarks>
public static class ChallengeErrors
{
    public static readonly Error NotFound = Error.NotFound("Challenge.NotFound", "The challenge was not found.");
    public static readonly Error AudienceMismatch = Error.Failure("Challenge.AudienceMismatch", "This challenge is for another audience.");
    public static readonly Error Full = Error.Conflict("Challenge.Full", "This challenge is full.");
    public static readonly Error NotOpen = Error.Conflict("Challenge.NotOpen", "This challenge is not open for joining.");
    public static readonly Error NoConsent = Error.Failure("Challenge.NoConsent", "A guardian must allow challenges first.");
    public static readonly Error NotGuardian = Error.Failure("Challenge.NotGuardian", "Only a guardian can do this.");
    public static readonly Error NoActiveLink = Error.Failure("Challenge.NoActiveLink", "No active support link to this learner.");
    public static readonly Error NotChild = Error.Validation("Challenge.NotChild", "Consent applies to child accounts only.");
    public static readonly Error InvalidTransition = Error.Conflict("Challenge.InvalidTransition", "The challenge cannot move to that status.");
    public static readonly Error InvalidSchedule = Error.Validation("Challenge.InvalidSchedule", "The challenge schedule or capacity is not valid.");

    /// <summary>Codes the API answers with 403 (the shared Kernel has no Forbidden type).</summary>
    public static readonly IReadOnlySet<string> ForbiddenCodes =
        new HashSet<string> { AudienceMismatch.Code, NoConsent.Code, NotGuardian.Code, NoActiveLink.Code };
}
