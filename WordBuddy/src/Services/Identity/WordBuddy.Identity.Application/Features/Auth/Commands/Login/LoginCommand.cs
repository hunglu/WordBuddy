using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;

namespace WordBuddy.Identity.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(string Email, string Password) : ICommand<AuthTokenDto>;
