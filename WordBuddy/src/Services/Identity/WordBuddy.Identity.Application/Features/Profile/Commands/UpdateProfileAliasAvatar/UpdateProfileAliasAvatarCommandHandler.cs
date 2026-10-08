using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.Caching;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Profile.Commands.UpdateProfileAliasAvatar;

/// <summary>Updates alias/avatar with the domain rules and a uniqueness check. Logs ids only, never the alias.</summary>
public sealed class UpdateProfileAliasAvatarCommandHandler : ICommandHandler<UpdateProfileAliasAvatarCommand, UserDto>
{
    private readonly IUserRepository _users;
    private readonly ISupportLinkRepository _links;
    private readonly IDistributedCache _cache;
    private readonly IValidator<UpdateProfileAliasAvatarCommand> _validator;
    private readonly ILogger<UpdateProfileAliasAvatarCommandHandler> _logger;

    public UpdateProfileAliasAvatarCommandHandler(
        IUserRepository users,
        ISupportLinkRepository links,
        IDistributedCache cache,
        IValidator<UpdateProfileAliasAvatarCommand> validator,
        ILogger<UpdateProfileAliasAvatarCommandHandler> logger)
    {
        _users = users;
        _links = links;
        _cache = cache;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<UserDto>> HandleAsync(UpdateProfileAliasAvatarCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("UpdateProfileAliasAvatarCommand started: UserId={UserId}", command.UserId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("UpdateProfileAliasAvatarCommand validation failed: UserId={UserId}", command.UserId);
            return Result.Failure<UserDto>(Error.Validation("UpdateProfileAliasAvatar.Validation", validation.ToString()));
        }

        Result<User> found = await _users.GetTrackedByIdAsync(command.UserId, ct);
        if (found.IsFailure)
        {
            return Result.Failure<UserDto>(found.Error);
        }

        User user = found.Value;
        string? alias = string.IsNullOrWhiteSpace(command.Alias) ? null : command.Alias.Trim();
        if (alias is not null && await _users.AliasTakenAsync(alias, user.Id, ct))
        {
            _logger.LogWarning("UpdateProfileAliasAvatarCommand conflict: alias taken, UserId={UserId}", command.UserId);
            return Result.Failure<UserDto>(UserErrors.AliasTaken);
        }

        Result set = user.SetAliasAndAvatar(alias, command.AvatarId);
        if (set.IsFailure)
        {
            _logger.LogWarning(
                "UpdateProfileAliasAvatarCommand rejected: UserId={UserId}, ErrorCode={ErrorCode}", command.UserId, set.Error.Code);
            return Result.Failure<UserDto>(set.Error);
        }

        Result save = await _users.SaveChangesAsync(ct);
        if (save.IsFailure)
        {
            return Result.Failure<UserDto>(save.Error);
        }

        // Linked users see the alias in their link lists.
        IReadOnlyList<SupportLink> links = await _links.GetLinksForUserAsync(user.Id, ct);
        List<Guid?> affected = [user.Id, .. links.SelectMany(l => new Guid?[] { l.LearnerId, l.SupporterId })];
        foreach (Guid learnerId in links.Where(l => l.LearnerId == user.Id).Select(l => l.LearnerId).Distinct())
        {
            affected.Add(await _links.GetActivePrimarySupporterIdAsync(learnerId, ct));
        }

        await SupportLinkCache.InvalidateAsync(_cache, ct, [.. affected]);

        bool hasActiveSupporter = await _links.HasActiveSupporterAsync(user.Id, ct);
        _logger.LogInformation("UpdateProfileAliasAvatarCommand succeeded: UserId={UserId}", command.UserId);
        return Result.Success(UserDto.From(user, hasActiveSupporter));
    }
}
