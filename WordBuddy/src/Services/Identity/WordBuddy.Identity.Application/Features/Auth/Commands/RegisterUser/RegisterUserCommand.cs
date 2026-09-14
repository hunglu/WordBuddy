using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Domain;

namespace WordBuddy.Identity.Application.Features.Auth.Commands.RegisterUser;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string DisplayName,
    AgeGroup AgeGroup) : ICommand<AuthTokenDto>;
