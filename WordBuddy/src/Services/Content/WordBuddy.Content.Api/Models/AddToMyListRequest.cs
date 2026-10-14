namespace WordBuddy.Content.Api.Models;

/// <summary>Optional body for the add-to-my-list endpoints (shared pool, auto-fill).</summary>
public sealed record AddToMyListRequest(string? PersonalContext);
