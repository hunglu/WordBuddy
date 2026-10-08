using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.AcceptInvitation;

/// <summary>Accepts an invitation by typed code or by link token (exactly one).</summary>
public sealed record AcceptInvitationCommand(Guid ActorId, string? Code, string? Token) : ICommand<SupportLinkDto>;
