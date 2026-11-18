using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Content.Infrastructure.Persistence;
using WordBuddy.Content.Infrastructure.Persistence.Dialects;
using WordBuddy.Content.Infrastructure.Persistence.Configurations;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Infrastructure.Repositories;

internal sealed class LexemeAdminRepository : ILexemeAdminRepository
{
    private readonly ContentDbContext _dbContext;
    private readonly ILogger<LexemeAdminRepository> _logger;
    private readonly IDatabaseDialect _dialect;

    public LexemeAdminRepository(ContentDbContext dbContext, ILogger<LexemeAdminRepository> logger, IDatabaseDialect dialect)
    {
        _dbContext = dbContext;
        _logger = logger;
        _dialect = dialect;
    }

    public async Task<Result<PagedResult<AdminLexemeListItemDto>>> SearchAsync(
        string? search, PartOfSpeech? partOfSpeech, CefrLevel? cefrLevel, int page, int pageSize, CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Searching lexemes: HasSearch={HasSearch}, PartOfSpeech={PartOfSpeech}, CefrLevel={CefrLevel}, Page={Page}, PageSize={PageSize}",
            !string.IsNullOrEmpty(search), partOfSpeech, cefrLevel, page, pageSize);

        IQueryable<Lexeme> query = _dbContext.Lexemes.AsNoTracking();

        // NormalizedLemma is stored upper-case; upper-casing the term keeps the match case-insensitive on
        // PostgreSQL (SQL Server already ignores case through its collation).
        string? escaped = string.IsNullOrWhiteSpace(search) ? null : LikePattern.Escape(search.ToUpperInvariant());
        string containsPattern = $"%{escaped}%";
        string prefixPattern = $"{escaped}%";
        if (escaped is not null)
        {
            query = query.Where(l => EF.Functions.Like(l.NormalizedLemma, containsPattern, LikePattern.EscapeCharacter));
        }

        if (partOfSpeech is not null)
        {
            PartOfSpeech pos = partOfSpeech.Value;
            query = query.Where(l => l.PartOfSpeech == pos);
        }

        if (cefrLevel is not null)
        {
            CefrLevel cefr = cefrLevel.Value;
            query = query.Where(l => l.CefrLevel == cefr);
        }

        int total = await query.CountAsync(ct);

        IOrderedQueryable<Lexeme> ordered = escaped is null
            ? query.OrderBy(l => l.NormalizedLemma)
            : query.OrderBy(l => EF.Functions.Like(l.NormalizedLemma, prefixPattern, LikePattern.EscapeCharacter) ? 0 : 1)
                   .ThenBy(l => l.NormalizedLemma);

        var rows = await ordered
            .ThenBy(l => l.PartOfSpeech)
            .ThenBy(l => l.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new
            {
                l.Id,
                l.NormalizedLemma,
                l.PartOfSpeech,
                l.IpaUk,
                l.IpaUs,
                l.CefrLevel,
                l.FrequencyRank,
                HasUkAudio = l.UkAudioAssetId != null,
                HasUsAudio = l.UsAudioAssetId != null,
                CatalogWord = _dbContext.Senses
                    .Where(s => s.LexemeId == l.Id
                                && (s.Source == VocabularySource.System || s.ShareStatus == VocabularyShareStatus.Shared)
                                && s.Word.Trim().ToUpper() == l.NormalizedLemma)
                    .OrderBy(s => s.CreatedAtUtc).ThenBy(s => s.Id)
                    .Select(s => s.Word)
                    .FirstOrDefault(),
                CatalogCount = _dbContext.Senses.Count(s => s.LexemeId == l.Id
                    && (s.Source == VocabularySource.System || s.ShareStatus == VocabularyShareStatus.Shared)),
                PrivateCount = _dbContext.Senses.Count(s => s.LexemeId == l.Id
                    && s.Source != VocabularySource.System && s.ShareStatus != VocabularyShareStatus.Shared),
            })
            .ToListAsync(ct);

        List<AdminLexemeListItemDto> items = rows
            .Select(r => new AdminLexemeListItemDto(
                r.Id,
                r.CatalogWord?.Trim(' ') ?? r.NormalizedLemma.ToLowerInvariant(),
                r.PartOfSpeech,
                r.IpaUk,
                r.IpaUs,
                r.CefrLevel,
                r.FrequencyRank,
                r.HasUkAudio,
                r.HasUsAudio,
                r.CatalogCount,
                r.PrivateCount))
            .ToList();

        return Result.Success(new PagedResult<AdminLexemeListItemDto>(items, page, pageSize, total));
    }

    public async Task<Result<LexemeDetailData>> GetDetailAsync(Guid id, CancellationToken ct = default)
    {
        Lexeme? lexeme = await _dbContext.Lexemes
            .AsNoTracking()
            .Include(l => l.UkAudio)
            .Include(l => l.UsAudio)
            .FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lexeme is null)
        {
            return Result.Failure<LexemeDetailData>(NotFound(id));
        }

        List<Sense> catalogSenses = await _dbContext.Senses
            .AsNoTracking()
            .Where(s => s.LexemeId == id
                        && (s.Source == VocabularySource.System || s.ShareStatus == VocabularyShareStatus.Shared))
            .OrderBy(s => s.CreatedAtUtc).ThenBy(s => s.Id)
            .ToListAsync(ct);

        int privateCount = await _dbContext.Senses
            .CountAsync(s => s.LexemeId == id
                             && s.Source != VocabularySource.System && s.ShareStatus != VocabularyShareStatus.Shared, ct);

        return Result.Success(new LexemeDetailData(lexeme, catalogSenses, privateCount));
    }

    public async Task<Result<Lexeme>> GetTrackedAsync(Guid id, CancellationToken ct = default)
    {
        Lexeme? lexeme = await _dbContext.Lexemes
            .AsTracking()
            .Include(l => l.UkAudio)
            .Include(l => l.UsAudio)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

        return lexeme is null ? Result.Failure<Lexeme>(NotFound(id)) : Result.Success(lexeme);
    }

    public async Task<Result<bool>> ExistsWithPartOfSpeechAsync(
        string normalizedLemma, PartOfSpeech partOfSpeech, Guid excludeId, CancellationToken ct = default)
    {
        bool exists = await _dbContext.Lexemes
            .AsNoTracking()
            .AnyAsync(l => l.NormalizedLemma == normalizedLemma && l.PartOfSpeech == partOfSpeech && l.Id != excludeId, ct);
        return Result.Success(exists);
    }

    public Result AddMediaAsset(MediaAsset asset)
    {
        _dbContext.MediaAssets.Add(asset);
        return Result.Success();
    }

    public async Task<Result> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (DbUpdateException ex) when (IsDuplicatePartOfSpeech(ex))
        {
            _dbContext.ChangeTracker.Clear();
            return Result.Failure(DuplicatePartOfSpeech());
        }
    }

    public async Task<Result<string?>> UnlinkAndDeleteAssetIfUnusedAsync(Lexeme lexeme, Guid? assetId, CancellationToken ct = default)
    {
        IExecutionStrategy strategy = _dbContext.Database.CreateExecutionStrategy();
        try
        {
            return await strategy.ExecuteAsync(async () =>
            {
                await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(ct);

                // Unlink first (and any new asset row the caller added), then the guarded delete.
                await _dbContext.SaveChangesAsync(ct);

                string? deletedUrl = null;
                if (assetId is not null)
                {
                    deletedUrl = await TryGuardedDeleteAsync(transaction, assetId.Value, ct);
                }

                await transaction.CommitAsync(ct);
                _logger.LogDebug("Lexeme audio unlinked: LexemeId={LexemeId}, AssetDeleted={AssetDeleted}", lexeme.Id, deletedUrl is not null);
                return Result.Success<string?>(deletedUrl);
            });
        }
        catch (DbUpdateException ex) when (IsDuplicatePartOfSpeech(ex))
        {
            _dbContext.ChangeTracker.Clear();
            return Result.Failure<string?>(DuplicatePartOfSpeech());
        }
    }

    public async Task<Result<bool>> IsUrlUsedAsync(string url, CancellationToken ct = default)
    {
        bool used = await _dbContext.MediaAssets.AsNoTracking().AnyAsync(m => m.Url == url, ct);
        return Result.Success(used);
    }

    private async Task<string?> TryGuardedDeleteAsync(IDbContextTransaction transaction, Guid assetId, CancellationToken ct)
    {
        DbConnection connection = _dbContext.Database.GetDbConnection();
        await using DbCommand command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = _dialect.GuardedMediaAssetDeleteSql;

        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = "@id";
        parameter.Value = assetId;
        command.Parameters.Add(parameter);

        // PostgreSQL aborts the whole transaction on any error, so the statement runs behind a savepoint.
        // Rolling back to it keeps the unlink that was already saved (on SQL Server this is harmless).
        const string savepoint = "guarded_media_delete";
        await transaction.CreateSavepointAsync(savepoint, ct);
        try
        {
            object? url = await command.ExecuteScalarAsync(ct);
            return url as string;
        }
        catch (DbException ex) when (_dialect.IsForeignKeyViolation(ex))
        {
            // A concurrent writer referenced the asset after the NOT EXISTS probes. The statement
            // fails alone: the unlink still commits, the row and the file stay.
            await transaction.RollbackToSavepointAsync(savepoint, ct);
            _logger.LogInformation("Media asset delete skipped: AssetId={AssetId} is referenced again", assetId);
            return null;
        }
    }

    private bool IsDuplicatePartOfSpeech(DbUpdateException ex) =>
        _dialect.IsUniqueViolation(ex, LexemeConfiguration.LemmaPartOfSpeechIndexName);

    private static Error DuplicatePartOfSpeech() =>
        Error.Conflict("Lexeme.DuplicatePartOfSpeech", "Another lexeme with the same word already has this part of speech.");

    private static Error NotFound(Guid id) => Error.NotFound("Lexeme.NotFound", $"Lexeme {id} was not found.");
}
