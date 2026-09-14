using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Auth.Commands.RegisterUser;

public sealed class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, AuthTokenDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IValidator<RegisterUserCommand> _validator;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IValidator<RegisterUserCommand> validator,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<AuthTokenDto>> HandleAsync(RegisterUserCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "RegisterUserCommand started: Email={Email}, AgeGroup={AgeGroup}",
            command.Email, command.AgeGroup);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("RegisterUserCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure<AuthTokenDto>(Error.Validation("RegisterUser.Validation", validation.ToString()));
        }

        if (await _userRepository.ExistsByEmailAsync(command.Email, ct))
        {
            _logger.LogWarning("RegisterUserCommand conflict: Email={Email} already registered", command.Email);
            return Result.Failure<AuthTokenDto>(Error.Conflict("RegisterUser.EmailTaken", $"Email '{command.Email}' is already registered."));
        }

        User user = new(
            Guid.NewGuid(),
            command.Email,
            command.DisplayName,
            _passwordHasher.Hash(command.Password),
            command.AgeGroup,
            isAdmin: false);

        Result addResult = await _userRepository.AddAsync(user, ct);
        if (addResult.IsFailure)
        {
            _logger.LogWarning(
                "RegisterUserCommand failed to persist user: {ErrorCode} — {ErrorDescription}",
                addResult.Error.Code, addResult.Error.Description);
            return Result.Failure<AuthTokenDto>(addResult.Error);
        }

        (string token, DateTime expiresAtUtc) = _jwtTokenGenerator.GenerateToken(user);
        UserDto userDto = new(user.Id, user.Email, user.DisplayName, user.AgeGroup, user.IsAdmin);

        _logger.LogInformation("RegisterUserCommand succeeded: UserId={UserId}", user.Id);
        return Result.Success(new AuthTokenDto(token, expiresAtUtc, userDto));
    }
}
