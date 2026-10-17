using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;

namespace WordBuddy.Identity.Application.Features.Profile.Commands.UpdatePreferredLanguage;

/// <summary>Sets or clears the caller preferred UI language.</summary>
public sealed record UpdatePreferredLanguageCommand(Guid UserId, string? Language) : ICommand<UserDto>;
