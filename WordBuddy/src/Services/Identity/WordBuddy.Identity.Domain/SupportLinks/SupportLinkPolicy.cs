using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Domain.SupportLinks;

/// <summary>
/// Domain rules for support links: supporter must be Adult; the first link of a child is Primary;
/// extra child links wait for the Primary; adult learners accept alone; the Primary link cannot be
/// unlinked; a child never acts on existing links (the Primary acts for the child).
/// </summary>
public static class SupportLinkPolicy
{
    /// <summary>Creates the link when an invitation is accepted.</summary>
    /// <param name="linkId">New link id.</param>
    /// <param name="learner">The learner.</param>
    /// <param name="supporter">The supporter.</param>
    /// <param name="relationship">Optional label.</param>
    /// <param name="learnerLinks">All existing links of the learner.</param>
    /// <param name="grantedBy">The accepting user.</param>
    /// <param name="nowUtc">Current time.</param>
    public static Result<SupportLink> CreateOnAccept(
        Guid linkId,
        LinkParty learner,
        LinkParty supporter,
        SupportRelationship? relationship,
        IReadOnlyCollection<SupportLink> learnerLinks,
        Guid grantedBy,
        DateTime nowUtc)
    {
        if (learner.UserId == supporter.UserId)
        {
            return Result.Failure<SupportLink>(SupportLinkErrors.SelfLink);
        }

        if (supporter.IsChild)
        {
            return Result.Failure<SupportLink>(SupportLinkErrors.SupporterMustBeAdult);
        }

        if (learnerLinks.Any(l => l.SupporterId == supporter.UserId && l.Status != SupportLinkStatus.Revoked))
        {
            return Result.Failure<SupportLink>(SupportLinkErrors.AlreadyLinked);
        }

        if (!learner.IsChild)
        {
            return Result.Success(SupportLink.Create(
                linkId, learner.UserId, supporter.UserId, isPrimary: false, relationship, SupportLinkStatus.Active, grantedBy, nowUtc));
        }

        bool hasPrimary = learnerLinks.Any(l => l.IsPrimary && l.IsActive);
        return hasPrimary
            ? Result.Success(SupportLink.Create(
                linkId, learner.UserId, supporter.UserId, isPrimary: false, relationship, SupportLinkStatus.PendingPrimaryApproval, grantedBy, nowUtc))
            : Result.Success(SupportLink.Create(
                linkId, learner.UserId, supporter.UserId, isPrimary: true, relationship, SupportLinkStatus.Active, grantedBy, nowUtc));
    }

    /// <summary>
    /// Returns the side <paramref name="actor"/> acts for on <paramref name="link"/>, or a
    /// Forbidden error. A child never acts; the side of a child learner is acted by the Primary.
    /// </summary>
    /// <param name="link">The link.</param>
    /// <param name="actor">The calling user.</param>
    /// <param name="learnerIsChild">Whether the learner of the link is a child.</param>
    /// <param name="learnerPrimarySupporterId">The active Primary supporter of the learner, if any.</param>
    public static Result<LinkSide> ResolveActorSide(
        SupportLink link,
        LinkParty actor,
        bool learnerIsChild,
        Guid? learnerPrimarySupporterId)
    {
        if (actor.IsChild)
        {
            return Result.Failure<LinkSide>(SupportLinkErrors.ChildForbidden);
        }

        if (actor.UserId == link.SupporterId)
        {
            return Result.Success(LinkSide.Supporter);
        }

        if (!learnerIsChild && actor.UserId == link.LearnerId)
        {
            return Result.Success(LinkSide.Learner);
        }

        if (learnerIsChild && learnerPrimarySupporterId == actor.UserId)
        {
            return Result.Success(LinkSide.Learner);
        }

        return Result.Failure<LinkSide>(SupportLinkErrors.Forbidden);
    }

    /// <summary>Checks that an unlink may be requested for <paramref name="link"/>.</summary>
    public static Result CanRequestUnlink(SupportLink link)
    {
        if (!link.IsActive)
        {
            return Result.Failure(SupportLinkErrors.InvalidStatus);
        }

        return link.IsPrimary ? Result.Failure(SupportLinkErrors.PrimaryCannotBeUnlinked) : Result.Success();
    }

    /// <summary>Checks that <paramref name="actor"/> may approve or reject a pending extra supporter.</summary>
    public static Result CanRespondToPending(SupportLink pendingLink, LinkParty actor, Guid? learnerPrimarySupporterId)
    {
        if (actor.IsChild)
        {
            return Result.Failure(SupportLinkErrors.ChildForbidden);
        }

        if (pendingLink.Status != SupportLinkStatus.PendingPrimaryApproval)
        {
            return Result.Failure(SupportLinkErrors.InvalidStatus);
        }

        return learnerPrimarySupporterId == actor.UserId ? Result.Success() : Result.Failure(SupportLinkErrors.Forbidden);
    }
}
