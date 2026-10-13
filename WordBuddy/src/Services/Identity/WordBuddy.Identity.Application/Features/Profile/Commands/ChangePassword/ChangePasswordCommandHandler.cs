using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Profile.Commands.ChangePassword;

/// <summary>Verifies the current password, stores the new hash, returns a fresh token. Never logs passwords or hashes.</summary>
public sealed class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand, AuthTokenDto>
{
    private readonly IUserRepository _users;
    private readonly ISupportLinkRepository _links;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenGenerator _jwt;
    private readonly TimeProvider _time;
    private readonly IValidator<ChangePasswordCommand> _validator;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(
        IUserRepository users,
        ISupportLinkRepository links,
        IPasswordHasher hasher,
        IJwtTokenGenerator jwt,
        TimeProvider time,
        IValidator<ChangePasswordCommand> validator,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _users = users;
        _links = links;
        _hasher = hasher;
        _jwt = jwt;
        _time = time;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<AuthTokenDto>> HandleAsync(ChangePasswordCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("ChangePasswordCommand started: UserId={UserId}", command.UserId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("ChangePasswordCommand validation failed: UserId={UserId}", command.UserId);
            return Result.Failure<AuthTokenDto>(Error.Validation("ChangePassword.Validation", validation.ToString()));
        }

        Result<User> found = await _users.GetTrackedByIdAsync(command.UserId, ct);
        if (found.IsFailure)
        {
            return Result.Failure<AuthTokenDto>(found.Error);
        }

        User user = found.Value;
        if (!_hasher.Verify(command.CurrentPassword, user.PasswordHash))
        {
            _logger.LogWarning("ChangePasswordCommand rejected: wrong current password, UserId={UserId}", command.UserId);
            return Result.Failure<AuthTokenDto>(UserErrors.WrongCurrentPassword);
        }

        Result set = user.ChangePassword(_hasher.Hash(command.NewPassword));
        if (set.IsFailure)
        {
            return Result.Failure<AuthTokenDto>(set.Error);
        }

        Result save = await _users.SaveChangesAsync(ct);
        if (save.IsFailure)
        {
            return Result.Failure<AuthTokenDto>(save.Error);
        }

        (string token, DateTime expiresAtUtc) = _jwt.GenerateToken(user);
        bool hasActiveSupporter = await _links.HasActiveSupporterAsync(user.Id, ct);

        _logger.LogInformation("ChangePasswordCommand succeeded: UserId={UserId}", command.UserId);
        return Result.Success(new AuthTokenDto(
            token, expiresAtUtc, UserDto.From(user, hasActiveSupporter, DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime))));
    }
}
