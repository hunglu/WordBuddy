using WordBuddy.Identity.Application.Abstractions;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.AdminHandoverPrimary;

/// <summary>Admin makes another active link of the learner the Primary one. Reason required (audited).</summary>
public sealed record AdminHandoverPrimaryCommand(Guid AdminId, Guid LearnerId, Guid NewPrimaryLinkId, string Reason) : ICommand;
