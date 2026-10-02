using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetSharedVocabularyWords;

/// <summary>High-read, low-mutation per CLAUDE.md's caching guidance — cached with a short absolute
/// expiry, cache-aside invalidated by <c>ModerateSharedVocabularyWordCommand</c> on success.</summary>
public sealed class GetSharedVocabularyWordsQueryHandler : IQueryHandler<GetSharedVocabularyWordsQuery, IReadOnlyList<PersonalVocabularyWordDto>>
{
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
    };

    private readonly IVocabularyWordRepository _repository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<GetSharedVocabularyWordsQueryHandler> _logger;

    public GetSharedVocabularyWordsQueryHandler(
        IVocabularyWordRepository repository,
        IDistributedCache cache,
        ILogger<GetSharedVocabularyWordsQueryHandler> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<PersonalVocabularyWordDto>>> HandleAsync(GetSharedVocabularyWordsQuery query, CancellationToken ct = default)
    {
        bool childSafeOnly = query.RequestingAgeGroup == AgeGroup.Child;
        string cacheKey = $"content:vocabulary-shared:{childSafeOnly}";

        _logger.LogInformation("GetSharedVocabularyWordsQuery started: ChildSafeOnly={ChildSafeOnly}", childSafeOnly);

        string? cached = await _cache.GetStringAsync(cacheKey, ct);
        if (cached is not null)
        {
            IReadOnlyList<PersonalVocabularyWordDto>? cachedDtos = JsonSerializer.Deserialize<IReadOnlyList<PersonalVocabularyWordDto>>(cached);
            if (cachedDtos is not null)
            {
                _logger.LogInformation("GetSharedVocabularyWordsQuery cache hit: ChildSafeOnly={ChildSafeOnly}", childSafeOnly);
                return Result.Success(cachedDtos);
            }
        }

        Result<IReadOnlyList<VocabularyWord>> wordsResult = await _repository.GetSharedAsync(childSafeOnly, ct);
        if (wordsResult.IsFailure)
        {
            _logger.LogWarning(
                "GetSharedVocabularyWordsQuery repository failure: {ErrorCode} — {ErrorDescription}",
                wordsResult.Error.Code, wordsResult.Error.Description);
            return Result.Failure<IReadOnlyList<PersonalVocabularyWordDto>>(wordsResult.Error);
        }

        IReadOnlyList<PersonalVocabularyWordDto> dtos = wordsResult.Value.Select(PersonalVocabularyWordMapper.ToDto).ToList();

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dtos), CacheOptions, ct);

        _logger.LogInformation("GetSharedVocabularyWordsQuery succeeded: Count={Count}, ChildSafeOnly={ChildSafeOnly}", dtos.Count, childSafeOnly);
        return Result.Success(dtos);
    }
}
