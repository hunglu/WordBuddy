using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Identity.Application.Caching;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Profile.Commands.UpdateDateOfBirth;

/// <summary>
/// Updates the date of birth with the domain rules. Logs ids only, never the date. No event is
/// published: the date stays in Identity and only the age band travels, in the token.
/// </summary>
public sealed class UpdateDateOfBirthCommandHandler : ICommandHandler<UpdateDateOfBirthCommand, AuthTokenDto>
{
    private readonly IUserRepository _users;
    private readonly ISupportLinkRepository _links;
    private readonly IDistributedCache _cache;
    private readonly IJwtTokenGenerator _jwt;
    private readonly TimeProvider _time;
    private readonly IValidator<UpdateDateOfBirthCommand> _validator;
    private readonly ILogger<UpdateDateOfBirthCommandHandler> _logger;

    public UpdateDateOfBirthCommandHandler(
        IUserRepository users,
        ISupportLinkRepository links,
        IDistributedCache cache,
        IJwtTokenGenerator jwt,
        TimeProvider time,
        IValidator<UpdateDateOfBirthCommand> validator,
        ILogger<UpdateDateOfBirthCommandHandler> logger)
    {
        _users = users;
        _links = links;
        _cache = cache;
        _jwt = jwt;
        _time = time;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<AuthTokenDto>> HandleAsync(UpdateDateOfBirthCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("UpdateDateOfBirthCommand started: UserId={UserId}", command.UserId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("UpdateDateOfBirthCommand validation failed: UserId={UserId}", command.UserId);
            return Result.Failure<AuthTokenDto>(Error.Validation("UpdateDateOfBirth.Validation", validation.ToString()));
        }

        Result<User> found = await _users.GetTrackedByIdAsync(command.UserId, ct);
        if (found.IsFailure)
        {
            return Result.Failure<AuthTokenDto>(found.Error);
        }

        User user = found.Value;
        DateOnly today = DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime);
        Result set = user.SetDateOfBirth(command.DateOfBirth, today);
        if (set.IsFailure)
        {
            _logger.LogWarning(
                "UpdateDateOfBirthCommand rejected: UserId={UserId}, ErrorCode={ErrorCode}", command.UserId, set.Error.Code);
            return Result.Failure<AuthTokenDto>(set.Error);
        }

        Result save = await _users.SaveChangesAsync(ct);
        if (save.IsFailure)
        {
            return Result.Failure<AuthTokenDto>(save.Error);
        }

        // Active supporters see the learner date of birth in their link list: drop their cached lists.
        IReadOnlyList<SupportLink> links = await _links.GetLinksForUserAsync(user.Id, ct);
        await SupportLinkCache.InvalidateAsync(
            _cache, ct, [.. links.Where(l => l.LearnerId == user.Id).Select(l => (Guid?)l.SupporterId)]);

        (string token, DateTime expiresAtUtc) = _jwt.GenerateToken(user);
        bool hasActiveSupporter = await _links.HasActiveSupporterAsync(user.Id, ct);

        _logger.LogInformation("UpdateDateOfBirthCommand succeeded: UserId={UserId}", command.UserId);
        return Result.Success(new AuthTokenDto(token, expiresAtUtc, UserDto.From(user, hasActiveSupporter, today)));
    }
}
