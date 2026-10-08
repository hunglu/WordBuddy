using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Domain.SupportLinks;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.CreateInvitation;

/// <summary>Creates an invitation code/link. A learner invites a supporter, or an adult invites a learner to support.</summary>
public sealed record CreateInvitationCommand(
    Guid ActorId,
    InvitationSide InviteAs,
    SupportRelationship? Relationship) : ICommand<CreatedInvitationDto>;
