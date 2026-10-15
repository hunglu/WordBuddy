using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Interfaces;

/// <summary>A lexeme with its audio assets and its catalog senses, for the admin detail view.
/// <paramref name="PrivateLearnerSenseCount"/> counts the other senses without loading them.</summary>
public sealed record LexemeDetailData(Lexeme Lexeme, IReadOnlyList<Sense> CatalogSenses, int PrivateLearnerSenseCount);

/// <summary>Persistence for the admin lexeme screen (WB-50).</summary>
public interface ILexemeAdminRepository
{
    /// <summary>Pages lexemes. <paramref name="search"/> is a "contains" match on the normalized
    /// lemma (already trimmed and upper-cased by the caller or empty); matches that start with the
    /// term come first.</summary>
    Task<Result<PagedResult<AdminLexemeListItemDto>>> SearchAsync(
        string? search, PartOfSpeech? partOfSpeech, CefrLevel? cefrLevel, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Reads one lexeme with audio and catalog senses (no tracking). <c>NotFound</c> when unknown.</summary>
    Task<Result<LexemeDetailData>> GetDetailAsync(Guid id, CancellationToken ct = default);

    /// <summary>Loads a tracked lexeme with both audio navigations, for a command. <c>NotFound</c> when unknown.</summary>
    Task<Result<Lexeme>> GetTrackedAsync(Guid id, CancellationToken ct = default);

    /// <summary>True when another lexeme (not <paramref name="excludeId"/>) has this lemma and part of speech.</summary>
    Task<Result<bool>> ExistsWithPartOfSpeechAsync(
        string normalizedLemma, PartOfSpeech partOfSpeech, Guid excludeId, CancellationToken ct = default);

    /// <summary>Adds a new asset to the context; it is written by the next save.</summary>
    Result AddMediaAsset(MediaAsset asset);

    /// <summary>Saves the tracked changes. A unique-index violation returns
    /// <c>Lexeme.DuplicatePartOfSpeech</c> (<c>Conflict</c>).</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>In one transaction: saves the tracked lexeme, then deletes the asset row
    /// <paramref name="assetId"/> only when no table references it any more. Returns the deleted
    /// row's <c>Url</c>, or <see langword="null"/> when the row stayed (still referenced, or none given).</summary>
    Task<Result<string?>> UnlinkAndDeleteAssetIfUnusedAsync(Lexeme lexeme, Guid? assetId, CancellationToken ct = default);

    /// <summary>True when any media asset row has this <paramref name="url"/>.</summary>
    Task<Result<bool>> IsUrlUsedAsync(string url, CancellationToken ct = default);
}
