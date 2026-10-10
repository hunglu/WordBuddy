using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;

namespace WordBuddy.Content.Application.Features.Groups.Commands.AssignWordsToGroup;

/// <summary>The group owner puts senses on the list of every active member, in one call.</summary>
public sealed record AssignWordsToGroupCommand(Guid GroupId, Guid CallerId, IReadOnlyList<Guid> SenseIds)
    : ICommand<GroupWordAssignmentResultDto>;
