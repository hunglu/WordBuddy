using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.Caching;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Application.Settings;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.CreateInvitation;

/// <summary>Creates an invitation. A child may only invite as learner (a child is never a supporter).</summary>
public sealed class CreateInvitationCommandHandler : ICommandHandler<CreateInvitationCommand, CreatedInvitationDto>
{
    private readonly IUserRepository _users;
    private readonly ISupportLinkRepository _links;
    private readonly IDistributedCache _cache;
    private readonly SupportLinkOptions _options;
    private readonly TimeProvider _time;
    private readonly IValidator<CreateInvitationCommand> _validator;
    private readonly ILogger<CreateInvitationCommandHandler> _logger;

    public CreateInvitationCommandHandler(
        IUserRepository users,
        ISupportLinkRepository links,
        IDistributedCache cache,
        SupportLinkOptions options,
        TimeProvider time,
        IValidator<CreateInvitationCommand> validator,
        ILogger<CreateInvitationCommandHandler> logger)
    {
        _users = users;
        _links = links;
        _cache = cache;
        _options = options;
        _time = time;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<CreatedInvitationDto>> HandleAsync(CreateInvitationCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "CreateInvitationCommand started: ActorId={ActorId}, InviteAs={InviteAs}", command.ActorId, command.InviteAs);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("CreateInvitationCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure<CreatedInvitationDto>(Error.Validation("CreateInvitation.Validation", validation.ToString()));
        }

        Result<User> actor = await _users.GetByIdAsync(command.ActorId, ct);
        if (actor.IsFailure)
        {
            return Result.Failure<CreatedInvitationDto>(actor.Error);
        }

        if (command.InviteAs == InvitationSide.Supporter && actor.Value.AgeGroup == AgeGroup.Child)
        {
            _logger.LogWarning("CreateInvitationCommand rejected: child cannot invite as supporter, ActorId={ActorId}", command.ActorId);
            return Result.Failure<CreatedInvitationDto>(SupportLinkErrors.SupporterMustBeAdult);
        }

        DateTime now = _time.GetUtcNow().UtcDateTime;
        string code = SupportLinkInvitation.GenerateCode();
        string token = SupportLinkInvitation.GenerateToken();
        SupportLinkInvitation invitation = SupportLinkInvitation.Create(
            Guid.NewGuid(), command.ActorId, command.InviteAs, command.Relationship, code, token, now,
            TimeSpan.FromDays(_options.InvitationExpiryDays));

        await _links.AddInvitationAsync(invitation, ct);
        Result save = await _links.SaveChangesAsync(ct);
        if (save.IsFailure)
        {
            _logger.LogWarning("CreateInvitationCommand failed to persist: {ErrorCode}", save.Error.Code);
            return Result.Failure<CreatedInvitationDto>(save.Error);
        }

        await SupportLinkCache.InvalidateAsync(_cache, ct, command.ActorId);

        _logger.LogInformation("CreateInvitationCommand succeeded: InvitationId={InvitationId}", invitation.Id);
        return Result.Success(new CreatedInvitationDto(invitation.Id, code, token, invitation.ExpiresAtUtc));
    }
}
