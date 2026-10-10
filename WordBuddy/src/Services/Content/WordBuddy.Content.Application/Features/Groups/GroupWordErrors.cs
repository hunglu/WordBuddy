using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Groups;

/// <summary>Typed errors of group word assignment. A code ending in <c>.Forbidden</c> maps to HTTP 403.</summary>
public static class GroupWordErrors
{
    public static readonly Error Forbidden = Error.Failure("GroupWords.Forbidden", "Only the group owner can do this.");
    public static readonly Error NoActiveMembers = Error.Validation("GroupWords.NoActiveMembers", "The group has no active members to assign words to.");
    public static readonly Error SenseNotFound = Error.NotFound("GroupWords.SenseNotFound", "One or more words were not found or cannot be assigned.");
}
