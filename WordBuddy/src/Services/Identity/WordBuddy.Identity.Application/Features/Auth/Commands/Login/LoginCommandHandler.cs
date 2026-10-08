using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, AuthTokenDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ISupportLinkRepository _supportLinks;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IValidator<LoginCommand> _validator;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IUserRepository userRepository,
        ISupportLinkRepository supportLinks,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IValidator<LoginCommand> validator,
        ILogger<LoginCommandHandler> logger)
    {
        _userRepository = userRepository;
        _supportLinks = supportLinks;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<AuthTokenDto>> HandleAsync(LoginCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("LoginCommand started: Email={Email}", command.Email);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("LoginCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure<AuthTokenDto>(Error.Validation("Login.Validation", validation.ToString()));
        }

        Result<User> userResult = await _userRepository.GetByEmailAsync(command.Email, ct);
        if (userResult.IsFailure || !_passwordHasher.Verify(command.Password, userResult.Value.PasswordHash))
        {
            _logger.LogWarning("LoginCommand failed: invalid credentials for Email={Email}", command.Email);
            return Result.Failure<AuthTokenDto>(Error.Validation("Login.InvalidCredentials", "Email or password is incorrect."));
        }

        User user = userResult.Value;
        (string token, DateTime expiresAtUtc) = _jwtTokenGenerator.GenerateToken(user);
        UserDto userDto = UserDto.From(user, await _supportLinks.HasActiveSupporterAsync(user.Id, ct));

        _logger.LogInformation("LoginCommand succeeded: UserId={UserId}", user.Id);
        return Result.Success(new AuthTokenDto(token, expiresAtUtc, userDto));
    }
}
