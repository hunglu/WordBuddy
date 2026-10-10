using WordBuddy.Identity.Application.Abstractions;

namespace WordBuddy.Identity.Application.Features.Groups.Commands.RenameGroup;

/// <summary>The owner renames a group.</summary>
public sealed record RenameGroupCommand(Guid GroupId, Guid CallerId, string Name) : ICommand;
