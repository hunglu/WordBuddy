using WordBuddy.Identity.Domain;

namespace WordBuddy.Identity.Api.Models;

/// <summary>Request body for registering a new user account. <c>TimeZoneId</c> is optional (IANA id, sent by the UI from the browser). <c>PreferredLanguage</c> is optional (for example <c>vi</c>).</summary>
public sealed record RegisterUserRequest(string Email, string Password, string DisplayName, AgeGroup AgeGroup, string? TimeZoneId = null, string? PreferredLanguage = null);
