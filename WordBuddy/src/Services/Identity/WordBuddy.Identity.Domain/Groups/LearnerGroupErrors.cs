using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Domain.Groups;

/// <summary>
/// Typed errors of the learning-groups feature. Codes ending in <c>.Forbidden</c> map to HTTP 403.
/// Descriptions hold no group names, aliases or child data.
/// </summary>
public static class LearnerGroupErrors
{
    public static readonly Error NotFound = Error.NotFound("LearnerGroup.NotFound", "The group was not found.");
    public static readonly Error MemberNotFound = Error.NotFound("LearnerGroup.MemberNotFound", "The group member was not found.");
    public static readonly Error LearnerNotFound = Error.NotFound("LearnerGroup.LearnerNotFound", "The learner was not found.");
    public static readonly Error NameLength = Error.Validation("LearnerGroup.NameLength", "The group name must be 3 to 60 characters.");
    public static readonly Error NotOwner = Error.Failure("LearnerGroup.Forbidden", "Only the group owner can do this.");
    public static readonly Error NotPrimary = Error.Failure("LearnerGroupMember.Forbidden", "Only the Primary supporter of the child can do this.");
    public static readonly Error ChildForbidden = Error.Failure("LearnerGroup.ChildForbidden", "A child account cannot do this.");
    public static readonly Error ChildCannotLeave = Error.Failure("LearnerGroupMember.ChildForbidden", "A child cannot leave a group. The Primary supporter removes the child.");
    public static readonly Error NoActiveLink = Error.Validation("LearnerGroup.NoActiveLink", "You do not actively support this learner.");
    public static readonly Error AlreadyMember = Error.Conflict("LearnerGroup.AlreadyMember", "The learner is already a member or waiting for approval.");
    public static readonly Error GroupFull = Error.Validation("LearnerGroup.GroupFull", "The group has reached its member limit.");
    public static readonly Error TooManyGroups = Error.Validation("LearnerGroup.TooManyGroups", "You have reached the group limit.");
    public static readonly Error InvalidStatus = Error.Conflict("LearnerGroup.InvalidStatus", "The membership is not in a state that allows this action.");
    public static readonly Error SelfAdd = Error.Validation("LearnerGroup.SelfAdd", "You cannot add yourself to your own group.");
}
