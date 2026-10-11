using WordBuddy.Identity.Domain;

namespace WordBuddy.Identity.Api.Models;

/// <summary>Request body for registering a new user account. <c>TimeZoneId</c> is optional (IANA id, sent by the UI from the browser).</summary>
public sealed record RegisterUserRequest(string Email, string Password, string DisplayName, AgeGroup AgeGroup, string? TimeZoneId = null);
