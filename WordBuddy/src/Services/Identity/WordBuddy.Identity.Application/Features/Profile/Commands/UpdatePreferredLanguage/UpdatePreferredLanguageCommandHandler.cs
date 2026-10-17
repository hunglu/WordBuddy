using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Profile.Commands.UpdatePreferredLanguage;

/// <summary>Updates the preferred UI language. Publishes no profile event: no consumer needs the language.</summary>
public sealed class UpdatePreferredLanguageCommandHandler : ICommandHandler<UpdatePreferredLanguageCommand, UserDto>
{
    private readonly IUserRepository _users;
    private readonly ISupportLinkRepository _links;
    private readonly IValidator<UpdatePreferredLanguageCommand> _validator;
    private readonly ILogger<UpdatePreferredLanguageCommandHandler> _logger;

    public UpdatePreferredLanguageCommandHandler(
        IUserRepository users,
        ISupportLinkRepository links,
        IValidator<UpdatePreferredLanguageCommand> validator,
        ILogger<UpdatePreferredLanguageCommandHandler> logger)
    {
        _users = users;
        _links = links;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<UserDto>> HandleAsync(UpdatePreferredLanguageCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("UpdatePreferredLanguageCommand started: UserId={UserId}", command.UserId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("UpdatePreferredLanguageCommand validation failed: UserId={UserId}", command.UserId);
            return Result.Failure<UserDto>(Error.Validation("UpdatePreferredLanguage.Validation", validation.ToString()));
        }

        Result<User> found = await _users.GetTrackedByIdAsync(command.UserId, ct);
        if (found.IsFailure)
        {
            return Result.Failure<UserDto>(found.Error);
        }

        User user = found.Value;
        Result set = user.SetPreferredLanguage(command.Language);
        if (set.IsFailure)
        {
            _logger.LogWarning(
                "UpdatePreferredLanguageCommand rejected: UserId={UserId}, ErrorCode={ErrorCode}", command.UserId, set.Error.Code);
            return Result.Failure<UserDto>(set.Error);
        }

        Result save = await _users.SaveChangesAsync(ct);
        if (save.IsFailure)
        {
            return Result.Failure<UserDto>(save.Error);
        }

        bool hasActiveSupporter = await _links.HasActiveSupporterAsync(user.Id, ct);
        _logger.LogInformation("UpdatePreferredLanguageCommand succeeded: UserId={UserId}", command.UserId);
        return Result.Success(UserDto.From(user, hasActiveSupporter));
    }
}
