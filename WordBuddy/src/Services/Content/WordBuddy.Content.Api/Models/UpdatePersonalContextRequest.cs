namespace WordBuddy.Content.Api.Models;

/// <summary>Request body to set, edit or clear (null or blank) the caller's private note on a word.</summary>
public sealed record UpdatePersonalContextRequest(string? PersonalContext);
