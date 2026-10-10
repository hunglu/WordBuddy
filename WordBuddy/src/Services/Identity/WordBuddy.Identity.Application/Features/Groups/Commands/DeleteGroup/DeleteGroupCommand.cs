using WordBuddy.Identity.Application.Abstractions;

namespace WordBuddy.Identity.Application.Features.Groups.Commands.DeleteGroup;

/// <summary>The owner deletes a group (soft delete). All members are removed.</summary>
public sealed record DeleteGroupCommand(Guid GroupId, Guid CallerId) : ICommand;
