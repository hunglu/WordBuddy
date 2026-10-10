using WordBuddy.Identity.Application.Abstractions;

namespace WordBuddy.Identity.Application.Features.Groups.Commands.LeaveGroup;

/// <summary>An adult learner leaves a group. A child cannot; the Primary supporter removes the child.</summary>
public sealed record LeaveGroupCommand(Guid GroupId, Guid LearnerId) : ICommand;
