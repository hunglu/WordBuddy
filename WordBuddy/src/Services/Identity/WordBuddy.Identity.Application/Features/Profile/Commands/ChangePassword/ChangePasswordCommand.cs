using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;

namespace WordBuddy.Identity.Application.Features.Profile.Commands.ChangePassword;

/// <summary>Changes the caller password after verifying the current one. Returns a fresh token.</summary>
public sealed record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : ICommand<AuthTokenDto>;
