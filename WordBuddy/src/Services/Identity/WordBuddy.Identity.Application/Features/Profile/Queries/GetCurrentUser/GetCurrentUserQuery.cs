using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;

namespace WordBuddy.Identity.Application.Features.Profile.Queries.GetCurrentUser;

/// <summary>The caller profile, including <see cref="UserDto.HasActiveSupporter"/> for the child gate in the UI.</summary>
public sealed record GetCurrentUserQuery(Guid UserId) : IQuery<UserDto>;
