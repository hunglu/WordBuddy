namespace WordBuddy.Progress.Infrastructure.Messaging;

/// <summary>Service-owned queue name for the Identity <c>LearnerProfileUpdated</c> event (see <see cref="SupportLinkQueues"/> for why).</summary>
public static class LearnerProfileQueues
{
    /// <summary>Queue for <c>LearnerProfileUpdated</c>.</summary>
    public const string Updated = "progress-learner-profile-updated";
}
