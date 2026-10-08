using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.SupportLinks;

namespace WordBuddy.Identity.Application.Features.SupportLinks;

/// <summary>Maps links to DTOs with public names only.</summary>
internal static class SupportLinkDtoMapper
{
    private const string UnknownName = "Unknown user";

    public static SupportLinkDto ToDto(
        SupportLink link,
        IReadOnlyDictionary<Guid, User> usersById,
        UnlinkRequest? openRequest,
        Guid callerId,
        int waitDays)
    {
        usersById.TryGetValue(link.LearnerId, out User? learner);
        usersById.TryGetValue(link.SupporterId, out User? supporter);

        UnlinkRequestDto? request = openRequest is null
            ? null
            : new UnlinkRequestDto(
                openRequest.Id,
                openRequest.Status,
                openRequest.RequestedById == callerId,
                openRequest.RequestedAtUtc,
                openRequest.EscalationAvailableAtUtc(waitDays));

        return new SupportLinkDto(
            link.Id,
            link.LearnerId,
            learner?.PublicName ?? UnknownName,
            learner?.AvatarId,
            link.SupporterId,
            supporter?.PublicName ?? UnknownName,
            supporter?.AvatarId,
            link.IsPrimary,
            link.Relationship,
            link.Status,
            link.CreatedAtUtc,
            request);
    }
}
