namespace WordBuddy.Identity.Domain.SupportLinks;

/// <summary>Which side of the future link created an invitation. The other side accepts it.</summary>
public enum InvitationSide
{
    /// <summary>The learner invites a supporter.</summary>
    Learner,

    /// <summary>The supporter invites a learner.</summary>
    Supporter
}
