namespace WordBuddy.Progress.Api.Models;

/// <summary>Body of <c>PUT /api/progress/challenges/consent/{childId}</c>.</summary>
/// <param name="Enabled">True allows challenges for the child; false stops them.</param>
public sealed record ChallengeConsentRequest(bool Enabled);
