using WordBuddy.Application.DTOs;
using WordBuddy.Domain.Common;

namespace WordBuddy.Application.Interfaces;

/// <summary>Provides user registration and credential verification services.</summary>
public interface IAuthService
{
    /// <summary>Registers a new user account. Returns a failure if the email is already taken.</summary>
    Task<Result<UserDto>> RegisterAsync(RegisterUserDto request, CancellationToken ct = default);

    /// <summary>Validates credentials and returns the user on success; returns a failure for invalid credentials.</summary>
    Task<Result<UserDto>> LoginAsync(LoginCredentialsDto credentials, CancellationToken ct = default);
}
