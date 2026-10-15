using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Content.Application.Caching;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetSharedVocabularyWords;

/// <summary>High-read, low-mutation per CLAUDE.md's caching guidance — cached with a short absolute
/// expiry, cache-aside invalidated by <c>ModerateSharedVocabularyWordCommand</c> and by a confirmed
/// author delete. The cache is per age group, not per user. After the cache read the handler filters by
/// search, slices one page, and only then sets <c>IsMine</c> on the page items.</summary>
public sealed class GetSharedVocabularyWordsQueryHandler : IQueryHandler<GetSharedVocabularyWordsQuery, PagedResult<PersonalVocabularyWordDto>>
{
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
    };

    private readonly IVocabularyWordRepository _repository;
    private readonly IDistributedCache _cache;
    private readonly IValidator<GetSharedVocabularyWordsQuery> _validator;
    private readonly ILogger<GetSharedVocabularyWordsQueryHandler> _logger;

    public GetSharedVocabularyWordsQueryHandler(
        IVocabularyWordRepository repository,
        IDistributedCache cache,
        IValidator<GetSharedVocabularyWordsQuery> validator,
        ILogger<GetSharedVocabularyWordsQueryHandler> logger)
    {
        _repository = repository;
        _cache = cache;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<PagedResult<PersonalVocabularyWordDto>>> HandleAsync(GetSharedVocabularyWordsQuery query, CancellationToken ct = default)
    {
        bool childSafeOnly = query.RequestingAgeGroup == AgeGroup.Child;
        string cacheKey = SharedVocabularyCacheKeys.For(childSafeOnly);

        _logger.LogInformation(
            "GetSharedVocabularyWordsQuery started: ChildSafeOnly={ChildSafeOnly}, HasSearch={HasSearch}, Page={Page}, PageSize={PageSize}",
            childSafeOnly, !string.IsNullOrWhiteSpace(query.Search), query.Page, query.PageSize);

        ValidationResult validation = await _validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("GetSharedVocabularyWordsQuery validation failed: {Errors}", validation.ToString());
            return Result.Failure<PagedResult<PersonalVocabularyWordDto>>(Error.Validation("GetSharedVocabularyWords.Validation", validation.ToString()));
        }

        string? cached = await _cache.GetStringAsync(cacheKey, ct);
        if (cached is not null)
        {
            IReadOnlyList<PersonalVocabularyWordDto>? cachedDtos = JsonSerializer.Deserialize<IReadOnlyList<PersonalVocabularyWordDto>>(cached);
            if (cachedDtos is not null)
            {
                _logger.LogInformation("GetSharedVocabularyWordsQuery cache hit: ChildSafeOnly={ChildSafeOnly}", childSafeOnly);
                return Result.Success(ToPage(cachedDtos, query));
            }
        }

        Result<IReadOnlyList<Sense>> wordsResult = await _repository.GetSharedAsync(childSafeOnly, ct);
        if (wordsResult.IsFailure)
        {
            _logger.LogWarning(
                "GetSharedVocabularyWordsQuery repository failure: {ErrorCode} — {ErrorDescription}",
                wordsResult.Error.Code, wordsResult.Error.Description);
            return Result.Failure<PagedResult<PersonalVocabularyWordDto>>(wordsResult.Error);
        }

        IReadOnlyList<PersonalVocabularyWordDto> dtos = wordsResult.Value.Select(PersonalVocabularyWordMapper.ToDto).ToList();

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dtos), CacheOptions, ct);

        _logger.LogInformation("GetSharedVocabularyWordsQuery succeeded: Count={Count}, ChildSafeOnly={ChildSafeOnly}", dtos.Count, childSafeOnly);
        return Result.Success(ToPage(dtos, query));
    }

    private static PagedResult<PersonalVocabularyWordDto> ToPage(IReadOnlyList<PersonalVocabularyWordDto> pool, GetSharedVocabularyWordsQuery query)
    {
        IReadOnlyList<PersonalVocabularyWordDto> matches = pool;
        string? term = query.Search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            matches = pool.Where(d => d.Word.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        PagedResult<PersonalVocabularyWordDto> page = Paging.Slice(matches, query.Page, query.PageSize);
        List<PersonalVocabularyWordDto> items = page.Items
            .Select(d => d with { IsMine = d.OwnerUserId == query.RequestingUserId })
            .ToList();
        return new PagedResult<PersonalVocabularyWordDto>(items, page.Page, page.PageSize, page.TotalCount);
    }
}
