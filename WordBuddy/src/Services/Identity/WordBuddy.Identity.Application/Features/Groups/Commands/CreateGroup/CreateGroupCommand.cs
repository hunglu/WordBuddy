using WordBuddy.Identity.Application.Abstractions;

namespace WordBuddy.Identity.Application.Features.Groups.Commands.CreateGroup;

/// <summary>An adult supporter creates a group. Returns the new group id.</summary>
public sealed record CreateGroupCommand(Guid OwnerId, string Name) : ICommand<Guid>;
