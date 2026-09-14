namespace WordBuddy.Identity.Api.Models;

/// <summary>Request body for authenticating an existing user.</summary>
public sealed record LoginRequest(string Email, string Password);
