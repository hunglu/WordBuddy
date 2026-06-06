using Microsoft.EntityFrameworkCore;
using WordBuddy.Application.DTOs;
using WordBuddy.Application.Interfaces;
using WordBuddy.Domain.Common;
using WordBuddy.Domain.Entities;
using WordBuddy.Domain.Enums;
using WordBuddy.Infrastructure.Persistence;

namespace WordBuddy.Infrastructure.Services;

internal sealed class AuthService : IAuthService
{
    private readonly WordBuddyDbContext _context;

    public AuthService(WordBuddyDbContext context) => _context = context;

    public async Task<Result<UserDto>> RegisterAsync(RegisterUserDto request, CancellationToken ct = default)
    {
        bool emailExists = await _context.Users
            .AnyAsync(u => u.Email == request.Email, ct);

        if (emailExists)
            return Result<UserDto>.Failure(
                Error.Conflict("User.EmailConflict", $"Email '{request.Email}' is already registered."));

        DateTime now = DateTime.UtcNow;
        User user = new(
            Guid.NewGuid(),
            request.Email,
            HashPassword(request.Password),
            request.DisplayName,
            request.AgeGroup,
            Level.Beginner,
            createdAt: now,
            updatedAt: now);

        await _context.Users.AddAsync(user, ct);
        await _context.SaveChangesAsync(ct);

        return Result<UserDto>.Success(MapToDto(user));
    }

    public async Task<Result<UserDto>> LoginAsync(LoginCredentialsDto credentials, CancellationToken ct = default)
    {
        User? user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == credentials.Email, ct);

        if (user is null || !VerifyPassword(credentials.Password, user.PasswordHash))
            return Result<UserDto>.Failure(
                Error.Failure("Auth.InvalidCredentials", "The email or password is incorrect."));

        return Result<UserDto>.Success(MapToDto(user));
    }

    private static UserDto MapToDto(User user) =>
        new(user.Id, user.Email, user.DisplayName, user.AgeGroup, user.Level);

    private static string HashPassword(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    private static bool VerifyPassword(string password, string storedHash) =>
        BCrypt.Net.BCrypt.Verify(password, storedHash);
}
