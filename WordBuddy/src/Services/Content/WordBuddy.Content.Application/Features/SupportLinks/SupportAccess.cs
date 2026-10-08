using WordBuddy.Content.Application.Interfaces;

namespace WordBuddy.Content.Application.Features.SupportLinks;

/// <summary>
/// Decisions behind the <c>CanSupportLearner</c> and <c>ChildHasSupporter</c> policies, on the local
/// <see cref="Domain.SupportLinkProjection"/>. The Api authorization handlers only read claims and call this.
/// </summary>
public sealed class SupportAccess
{
    private readonly ISupportLinkProjectionRepository _links;

    public SupportAccess(ISupportLinkProjectionRepository links)
    {
        _links = links;
    }

    /// <summary>True when <paramref name="callerId"/> has an active link to <paramref name="learnerId"/>. Inactive or missing → false.</summary>
    public async Task<bool> CanSupportLearnerAsync(Guid? callerId, Guid? learnerId, CancellationToken ct = default) =>
        callerId is { } caller && learnerId is { } learner && await _links.HasActiveLinkAsync(caller, learner, ct);

    /// <summary>Adults and admins pass; a child passes only with at least one active supporter.</summary>
    public async Task<bool> ChildHasSupporterAsync(Guid? callerId, bool isChild, bool isAdmin, CancellationToken ct = default)
    {
        if (isAdmin || !isChild)
        {
            return true;
        }

        return callerId is { } caller && await _links.HasActiveSupporterAsync(caller, ct);
    }
}
