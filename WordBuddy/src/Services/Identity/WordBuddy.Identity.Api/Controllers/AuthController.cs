using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WordBuddy.Identity.Api.Extensions;
using WordBuddy.Identity.Api.Models;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Features.Auth.Commands.Login;
using WordBuddy.Identity.Application.Features.Auth.Commands.RegisterUser;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Api.Controllers;

/// <summary>Registration and login for WordBuddy accounts.</summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ICommandHandler<RegisterUserCommand, AuthTokenDto> _registerUser;
    private readonly ICommandHandler<LoginCommand, AuthTokenDto> _login;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ICommandHandler<RegisterUserCommand, AuthTokenDto> registerUser,
        ICommandHandler<LoginCommand, AuthTokenDto> login,
        ILogger<AuthController> logger)
    {
        _registerUser = registerUser;
        _login = login;
        _logger = logger;
    }

    /// <summary>Registers a new account and returns a JWT for it.</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest request, CancellationToken ct)
    {
        Result<AuthTokenDto> result = await _registerUser.HandleAsync(
            new RegisterUserCommand(request.Email, request.Password, request.DisplayName, request.AgeGroup), ct);

        if (result.IsFailure)
        {
            return result.ToProblemResult(this);
        }

        _logger.LogInformation("User registered: UserId={UserId}", result.Value.User.Id);
        return CreatedAtAction(nameof(Register), result.Value);
    }

    /// <summary>Authenticates an existing account and returns a JWT for it.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        Result<AuthTokenDto> result = await _login.HandleAsync(new LoginCommand(request.Email, request.Password), ct);

        if (result.IsFailure)
        {
            _logger.LogWarning("Login failed for Email={Email}", request.Email);
            return result.ToProblemResult(this);
        }

        _logger.LogInformation("User logged in: UserId={UserId}", result.Value.User.Id);
        return Ok(result.Value);
    }
}
