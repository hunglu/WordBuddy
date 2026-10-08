namespace WordBuddy.Content.Infrastructure.Messaging;

/// <summary>
/// Service-owned queue names for the Identity support-link events. Content and Progress both consume
/// these events; with the default (consumer-name) endpoint names they would share one queue and
/// each event would reach only one service (round robin). A service prefix gives each its own copy.
/// </summary>
public static class SupportLinkQueues
{
    /// <summary>Queue for <c>SupportLinkActivated</c>.</summary>
    public const string Activated = "content-support-link-activated";

    /// <summary>Queue for <c>SupportLinkRevoked</c>.</summary>
    public const string Revoked = "content-support-link-revoked";
}
