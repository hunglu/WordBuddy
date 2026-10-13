using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;

namespace WordBuddy.Identity.Application.Features.Profile.Commands.UpdateDateOfBirth;

/// <summary>Sets or clears the caller date of birth. Returns a new token so the <c>age_band</c> claim refreshes.</summary>
public sealed record UpdateDateOfBirthCommand(Guid UserId, DateOnly? DateOfBirth) : ICommand<AuthTokenDto>;
