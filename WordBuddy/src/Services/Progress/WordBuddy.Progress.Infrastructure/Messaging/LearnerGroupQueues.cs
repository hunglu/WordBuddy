namespace WordBuddy.Progress.Infrastructure.Messaging;

/// <summary>
/// Service-owned queue names for the Identity group events. Content and Progress both consume
/// them; a service prefix gives each its own copy of every event (see <see cref="SupportLinkQueues"/>).
/// </summary>
public static class LearnerGroupQueues
{
    /// <summary>Queue for <c>LearnerGroupMemberActivated</c>.</summary>
    public const string MemberActivated = "progress-learner-group-member-activated";

    /// <summary>Queue for <c>LearnerGroupMemberRemoved</c>.</summary>
    public const string MemberRemoved = "progress-learner-group-member-removed";

    /// <summary>Queue for <c>LearnerGroupDeleted</c>.</summary>
    public const string GroupDeleted = "progress-learner-group-deleted";
}
