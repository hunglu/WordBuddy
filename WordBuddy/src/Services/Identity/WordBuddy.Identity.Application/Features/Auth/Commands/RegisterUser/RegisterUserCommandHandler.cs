using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Shared.Application.Abstractions;
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
    private readonly ILearnerProfileEventPublisher _profileEvents;
    private readonly TimeProvider _time;
    private readonly IValidator<RegisterUserCommand> _validator;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        ILearnerProfileEventPublisher profileEvents,
        TimeProvider time,
        IValidator<RegisterUserCommand> validator,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _profileEvents = profileEvents;
        _time = time;
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

        Result zone = user.SetTimeZone(command.TimeZoneId);
        if (zone.IsFailure)
        {
            _logger.LogWarning("RegisterUserCommand rejected: ErrorCode={ErrorCode}", zone.Error.Code);
            return Result.Failure<AuthTokenDto>(zone.Error);
        }

        // Staged in the outbox: the event commits together with the new user.
        await _profileEvents.PublishAsync(user, _time.GetUtcNow().UtcDateTime, ct);

        Result addResult = await _userRepository.AddAsync(user, ct);
        if (addResult.IsFailure)
        {
            _logger.LogWarning(
                "RegisterUserCommand failed to persist user: {ErrorCode} — {ErrorDescription}",
                addResult.Error.Code, addResult.Error.Description);
            return Result.Failure<AuthTokenDto>(addResult.Error);
        }

        (string token, DateTime expiresAtUtc) = _jwtTokenGenerator.GenerateToken(user);
        UserDto userDto = UserDto.From(user, hasActiveSupporter: false);

        _logger.LogInformation("RegisterUserCommand succeeded: UserId={UserId}", user.Id);
        return Result.Success(new AuthTokenDto(token, expiresAtUtc, userDto));
    }
}
