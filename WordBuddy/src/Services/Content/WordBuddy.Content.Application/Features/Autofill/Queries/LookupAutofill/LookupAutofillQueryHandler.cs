using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.Caching;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Application.Interfaces.Autofill;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Autofill.Queries.LookupAutofill;

/// <summary>Auto-fill lookup:
/// <list type="number">
/// <item>Catalog (cache, then database): stored auto-fill senses → returned, <b>no external call</b>.</item>
/// <item>Negative cache: the dictionary did not know the word in the last hour → unavailable.</item>
/// <item>Dictionary → generator → save → catalog re-read.</item>
/// </list>
/// Any external failure returns <c>AutofillUnavailable = true</c> and saves nothing. Logs never contain the word.</summary>
public sealed class LookupAutofillQueryHandler : IQueryHandler<LookupAutofillQuery, AutofillResultDto>
{
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = AutofillCacheKeys.Lifetime,
    };

    private readonly IAutofillRepository _autofillRepository;
    private readonly IVocabularyWordRepository _vocabularyRepository;
    private readonly IDictionaryClient _dictionary;
    private readonly ISenseGenerator _generator;
    private readonly AutofillCatalogWriter _writer;
    private readonly AutofillSettings _settings;
    private readonly IDistributedCache _cache;
    private readonly IValidator<LookupAutofillQuery> _validator;
    private readonly ILogger<LookupAutofillQueryHandler> _logger;

    public LookupAutofillQueryHandler(
        IAutofillRepository autofillRepository,
        IVocabularyWordRepository vocabularyRepository,
        IDictionaryClient dictionary,
        ISenseGenerator generator,
        AutofillCatalogWriter writer,
        AutofillSettings settings,
        IDistributedCache cache,
        IValidator<LookupAutofillQuery> validator,
        ILogger<LookupAutofillQueryHandler> logger)
    {
        _autofillRepository = autofillRepository;
        _vocabularyRepository = vocabularyRepository;
        _dictionary = dictionary;
        _generator = generator;
        _writer = writer;
        _settings = settings;
        _cache = cache;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<AutofillResultDto>> HandleAsync(LookupAutofillQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("LookupAutofillQuery started: AgeGroup={AgeGroup}", query.RequestingAgeGroup);

        ValidationResult validation = await _validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("LookupAutofillQuery validation failed: {Errors}", validation.ToString());
            return Result.Failure<AutofillResultDto>(Error.Validation("LookupAutofill.Validation", validation.ToString()));
        }

        string word = CollapseSpaces(query.Word);
        string normalized = Sense.NormalizeWord(word);

        Result<AutofillResultDto?> catalog = await ReadCatalogAsync(word, normalized, ct);
        if (catalog.IsFailure)
        {
            return Result.Failure<AutofillResultDto>(catalog.Error);
        }

        if (catalog.Value is { } hit)
        {
            _logger.LogInformation("LookupAutofillQuery catalog hit: SenseCount={SenseCount}", hit.Senses.Count);
            return Result.Success(await ForCallerAsync(hit, query, ct));
        }

        if (!string.IsNullOrEmpty(await _cache.GetStringAsync(AutofillCacheKeys.Miss(normalized), ct)))
        {
            _logger.LogInformation("LookupAutofillQuery negative cache hit");
            return Result.Success(AutofillResultDto.Unavailable(word));
        }

        Result<DictionaryEntry> entry = await _dictionary.LookupAsync(word, ct);
        if (entry.IsFailure)
        {
            if (entry.Error.Type == ErrorType.NotFound)
            {
                await _cache.SetStringAsync(AutofillCacheKeys.Miss(normalized), "1", CacheOptions, ct);
            }

            _logger.LogWarning("LookupAutofillQuery dictionary failure: {ErrorCode}", entry.Error.Code);
            return Result.Success(AutofillResultDto.Unavailable(word));
        }

        string lemma = Sense.NormalizeWord(entry.Value.Word) == normalized ? entry.Value.Word.Trim() : word;

        Result<IReadOnlyList<Sense>> movable = await _autofillRepository.GetMovableCatalogSensesAsync(normalized, ct);
        if (movable.IsFailure)
        {
            return Result.Failure<AutofillResultDto>(movable.Error);
        }

        Result<GeneratedWord> generated = await _generator.GenerateAsync(
            new SenseGenerationRequest(
                lemma,
                entry.Value.PartsOfSpeech,
                _settings.TranslationLocales,
                movable.Value.Select(s => new ExistingCatalogSense(s.Id, s.Definition)).ToList()),
            ct);
        if (generated.IsFailure)
        {
            _logger.LogWarning("LookupAutofillQuery generator failure: {ErrorCode}", generated.Error.Code);
            return Result.Success(AutofillResultDto.Unavailable(word));
        }

        Result saved = await _writer.SaveAsync(lemma, entry.Value, generated.Value, movable.Value, ct);
        if (saved.IsFailure && saved.Error.Type != ErrorType.Conflict)
        {
            _logger.LogWarning("LookupAutofillQuery save failed: {ErrorCode}", saved.Error.Code);
            return Result.Success(AutofillResultDto.Unavailable(word));
        }

        // Saved, or a concurrent request saved first (Conflict): read the catalog either way.
        await InvalidateAsync(normalized, ct);
        Result<AutofillResultDto?> reread = await ReadCatalogAsync(word, normalized, ct);
        if (reread.IsFailure || reread.Value is null)
        {
            _logger.LogWarning("LookupAutofillQuery found no catalog entry after save");
            return Result.Success(AutofillResultDto.Unavailable(word));
        }

        _logger.LogInformation(
            "LookupAutofillQuery succeeded: SenseCount={SenseCount}, Concurrent={Concurrent}",
            reread.Value.Senses.Count, saved.IsFailure);
        return Result.Success(await ForCallerAsync(reread.Value, query, ct));
    }

    /// <summary>Cache first, then database. <see langword="null"/> value = not in the catalog.</summary>
    private async Task<Result<AutofillResultDto?>> ReadCatalogAsync(string word, string normalized, CancellationToken ct)
    {
        string key = AutofillCacheKeys.Catalog(normalized);
        string? cached = await _cache.GetStringAsync(key, ct);
        if (!string.IsNullOrEmpty(cached) && TryDeserialize(cached) is { Senses.Count: > 0 } fromCache)
        {
            return Result.Success<AutofillResultDto?>(fromCache);
        }

        Result<IReadOnlyList<Sense>> senses = await _autofillRepository.GetCatalogAsync(normalized, ct);
        if (senses.IsFailure)
        {
            return Result.Failure<AutofillResultDto?>(senses.Error);
        }

        if (senses.Value.Count == 0)
        {
            return Result.Success<AutofillResultDto?>(null);
        }

        AutofillResultDto dto = new(word, false, senses.Value.Select(AutofillSenseDto.From).ToList());
        await _cache.SetStringAsync(key, JsonSerializer.Serialize(dto), CacheOptions, ct);
        return Result.Success<AutofillResultDto?>(dto);
    }

    /// <summary>Adults get the full result. A Child gets content only for senses approved globally
    /// or through their own link; the cache always holds the full (unmasked) result.</summary>
    private async Task<AutofillResultDto> ForCallerAsync(AutofillResultDto dto, LookupAutofillQuery query, CancellationToken ct)
    {
        if (query.RequestingAgeGroup != AgeGroup.Child)
        {
            return dto;
        }

        List<Guid> ids = dto.Senses.Select(s => s.SenseId).ToList();
        Result<IReadOnlyList<SenseReviewCandidate>> candidates = await _vocabularyRepository.GetForReviewAsync(query.RequestingUserId, ids, ct);
        Dictionary<Guid, SenseReviewCandidate> byId = candidates.IsSuccess
            ? candidates.Value.ToDictionary(c => c.Sense.Id)
            : [];

        return dto with
        {
            Senses = dto.Senses
                .Select(s => byId.TryGetValue(s.SenseId, out SenseReviewCandidate? c) && c.Sense.IsVisibleTo(query.RequestingUserId, AgeGroup.Child, c.CallerLink)
                    ? s
                    : s.Masked())
                .ToList(),
        };
    }

    private async Task InvalidateAsync(string normalized, CancellationToken ct)
    {
        await _cache.RemoveAsync(AutofillCacheKeys.Catalog(normalized), ct);
        await _cache.RemoveAsync(AutofillCacheKeys.Miss(normalized), ct);
        await _cache.RemoveAsync(SharedVocabularyCacheKeys.ChildSafe, ct);
        await _cache.RemoveAsync(SharedVocabularyCacheKeys.All, ct);
    }

    /// <summary>A corrupt cache entry counts as a miss.</summary>
    private static AutofillResultDto? TryDeserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<AutofillResultDto>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string CollapseSpaces(string word) =>
        string.Join(' ', word.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
