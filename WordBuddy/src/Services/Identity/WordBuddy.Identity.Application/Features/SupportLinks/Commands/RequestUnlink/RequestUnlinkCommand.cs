using WordBuddy.Identity.Application.Abstractions;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.RequestUnlink;

/// <summary>One side of an active link asks to end it. The Primary acts for a child learner; a child cannot request.</summary>
public sealed record RequestUnlinkCommand(Guid ActorId, Guid LinkId) : ICommand;
