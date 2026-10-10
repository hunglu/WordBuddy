using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;

namespace WordBuddy.Identity.Application.Features.Groups.Commands.AddGroupMembers;

/// <summary>The owner adds learners they actively support. Returns one outcome per learner.</summary>
public sealed record AddGroupMembersCommand(Guid GroupId, Guid CallerId, IReadOnlyList<Guid> LearnerIds)
    : ICommand<IReadOnlyList<AddMemberResultDto>>;
