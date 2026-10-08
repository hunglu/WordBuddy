using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.SupportLinks;

/// <summary>The caller resolved against a link: their side, plus the Primary of the learner (for cache invalidation).</summary>
public sealed record ResolvedLinkActor(LinkParty Actor, LinkSide Side, Guid? LearnerPrimarySupporterId);

/// <summary>Loads the caller and the learner of a link and applies <see cref="SupportLinkPolicy.ResolveActorSide"/>.</summary>
public sealed class LinkActorResolver
{
    private readonly IUserRepository _users;
    private readonly ISupportLinkRepository _links;

    public LinkActorResolver(IUserRepository users, ISupportLinkRepository links)
    {
        _users = users;
        _links = links;
    }

    /// <summary>Returns the caller side on <paramref name="link"/>, or a Forbidden / NotFound error.</summary>
    public async Task<Result<ResolvedLinkActor>> ResolveAsync(Guid actorId, SupportLink link, CancellationToken ct)
    {
        Result<User> actor = await _users.GetByIdAsync(actorId, ct);
        if (actor.IsFailure)
        {
            return Result.Failure<ResolvedLinkActor>(actor.Error);
        }

        Result<User> learner = await _users.GetByIdAsync(link.LearnerId, ct);
        if (learner.IsFailure)
        {
            return Result.Failure<ResolvedLinkActor>(learner.Error);
        }

        bool learnerIsChild = learner.Value.AgeGroup == AgeGroup.Child;
        Guid? primaryId = learnerIsChild ? await _links.GetActivePrimarySupporterIdAsync(link.LearnerId, ct) : null;

        LinkParty party = new(actor.Value.Id, actor.Value.AgeGroup);
        Result<LinkSide> side = SupportLinkPolicy.ResolveActorSide(link, party, learnerIsChild, primaryId);

        return side.IsFailure
            ? Result.Failure<ResolvedLinkActor>(side.Error)
            : Result.Success(new ResolvedLinkActor(party, side.Value, primaryId));
    }
}
