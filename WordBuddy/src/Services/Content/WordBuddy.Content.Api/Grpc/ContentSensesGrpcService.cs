using System.Security.Claims;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using WordBuddy.Content.Api.Authorization;
using WordBuddy.Content.Api.Extensions;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetSensesByIds;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;
using WordBuddy.Shared.Protos.Content.V1;

namespace WordBuddy.Content.Api.Grpc;

/// <summary>gRPC entry point for internal callers (Progress). Same security as
/// <c>PersonalVocabularyController</c>: JWT from the <c>authorization</c> metadata, the
/// <c>ChildHasSupporter</c> policy, and user id and age group read from the token only.</summary>
[Authorize]
[Authorize(Policy = SupportLinkPolicies.ChildHasSupporter)]
public sealed class ContentSensesGrpcService : ContentSenses.ContentSensesBase
{
    private readonly IQueryHandler<GetSensesByIdsQuery, IReadOnlyList<SenseReviewDto>> _getSensesByIds;
    private readonly ILogger<ContentSensesGrpcService> _logger;

    public ContentSensesGrpcService(
        IQueryHandler<GetSensesByIdsQuery, IReadOnlyList<SenseReviewDto>> getSensesByIds,
        ILogger<ContentSensesGrpcService> logger)
    {
        _getSensesByIds = getSensesByIds;
        _logger = logger;
    }

    /// <summary>Batch read of senses for a review session. Hidden and unknown ids are omitted.</summary>
    public override async Task<GetSensesByIdsReply> GetSensesByIds(GetSensesByIdsRequest request, ServerCallContext context)
    {
        var ids = new List<Guid>(request.SenseIds.Count);
        foreach (string raw in request.SenseIds)
        {
            if (!Guid.TryParse(raw, out Guid id))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Sense.InvalidId"));
            }

            ids.Add(id);
        }

        ClaimsPrincipal user = context.GetHttpContext().User;
        Result<IReadOnlyList<SenseReviewDto>> result = await _getSensesByIds.HandleAsync(
            new GetSensesByIdsQuery(ids, user.GetUserId(), user.GetAgeGroup()), context.CancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("GetSensesByIds failed: Code={ErrorCode}, Count={Count}", result.Error.Code, ids.Count);
            throw result.Error.ToRpcException();
        }

        var reply = new GetSensesByIdsReply();
        foreach (SenseReviewDto sense in result.Value)
        {
            reply.Senses.Add(ToMessage(sense));
        }

        return reply;
    }

    private static SenseReview ToMessage(SenseReviewDto dto)
    {
        var message = new SenseReview
        {
            SenseId = dto.SenseId.ToString(),
            Word = dto.Word,
            Definition = dto.Definition,
        };

        if (dto.Example is not null)
        {
            message.Example = dto.Example;
        }

        if (dto.AudioUrl is not null)
        {
            message.AudioUrl = dto.AudioUrl;
        }

        if (dto.ImageUrl is not null)
        {
            message.ImageUrl = dto.ImageUrl;
        }

        if (dto.PersonalContext is not null)
        {
            message.PersonalContext = dto.PersonalContext;
        }

        return message;
    }
}
