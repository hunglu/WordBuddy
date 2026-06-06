using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WordBuddy.API.Models.Requests;
using WordBuddy.API.Services;
using WordBuddy.Application.DTOs;
using WordBuddy.Application.Interfaces;
using WordBuddy.Domain.Common;

namespace WordBuddy.API.Controllers;

/// <summary>Provides endpoints for user registration and authentication.</summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly JwtTokenGenerator _tokenGenerator;

    /// <summary>Initializes a new <see cref="AuthController"/>.</summary>
    public AuthController(IAuthService authService, JwtTokenGenerator tokenGenerator)
    {
        _authService = authService;
        _tokenGenerator = tokenGenerator;
    }

    /// <summary>Registers a new user account and returns a JWT access token.</summary>
    /// <param name="request">The registration details.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType<AuthTokenDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest request, CancellationToken ct)
    {
        Result<UserDto> result = await _authService.RegisterAsync(
            new RegisterUserDto(request.Email, request.Password, request.DisplayName, request.AgeGroup),
            ct);

        if (result.IsFailure)
            return result.Error.Code.Contains("Conflict") ? Conflict(result.Error) : BadRequest(result.Error);

        AuthTokenDto token = _tokenGenerator.Generate(result.Value);
        return StatusCode(StatusCodes.Status201Created, token);
    }

    /// <summary>Authenticates a user and returns a JWT access token.</summary>
    /// <param name="request">The login credentials.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<AuthTokenDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        Result<UserDto> result = await _authService.LoginAsync(
            new LoginCredentialsDto(request.Email, request.Password),
            ct);

        if (result.IsFailure)
            return Unauthorized(result.Error);

        AuthTokenDto token = _tokenGenerator.Generate(result.Value);
        return Ok(token);
    }
}
