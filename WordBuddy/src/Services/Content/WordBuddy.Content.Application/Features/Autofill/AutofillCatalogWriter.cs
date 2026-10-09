using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Application.Interfaces.Autofill;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Autofill;

/// <summary>The auto-fill save step: one lexeme per part of speech, its senses and translations,
/// lexeme audio (<c>lexeme-{id}-en-GB.mp3</c> / <c>-en-US.mp3</c>), and the move of matched System or
/// Shared senses off the null-part-of-speech lexeme. Private learner senses are never touched.</summary>
public sealed class AutofillCatalogWriter
{
    private const string AudioContentType = "audio/mpeg";

    private readonly IAutofillRepository _repository;
    private readonly IAudioDownloader _audioDownloader;
    private readonly IFileStorageService _fileStorage;
    private readonly AutofillSettings _settings;
    private readonly ILogger<AutofillCatalogWriter> _logger;

    public AutofillCatalogWriter(
        IAutofillRepository repository,
        IAudioDownloader audioDownloader,
        IFileStorageService fileStorage,
        AutofillSettings settings,
        ILogger<AutofillCatalogWriter> logger)
    {
        _repository = repository;
        _audioDownloader = audioDownloader;
        _fileStorage = fileStorage;
        _settings = settings;
        _logger = logger;
    }

    /// <summary>Builds and saves the catalog entries. <paramref name="movableSenses"/> must be tracked
    /// (from <see cref="IAutofillRepository.GetMovableCatalogSensesAsync"/>). Returns
    /// <c>Conflict</c> on a concurrent save of the same lemma.</summary>
    public async Task<Result> SaveAsync(
        string lemma,
        DictionaryEntry entry,
        GeneratedWord generated,
        IReadOnlyList<Sense> movableSenses,
        CancellationToken ct = default)
    {
        string normalized = Sense.NormalizeWord(lemma);
        List<Lexeme> lexemes = [];
        List<Sense> senses = [];
        List<Sense> moved = [];
        Dictionary<Guid, Sense> movableById = movableSenses
            .Where(s => s.Source == VocabularySource.System || s.ShareStatus == VocabularyShareStatus.Shared)
            .ToDictionary(s => s.Id);

        foreach (GeneratedLexeme generatedLexeme in generated.Lexemes)
        {
            Result<Lexeme> lexemeResult = Lexeme.Create(Guid.NewGuid(), lemma, generatedLexeme.PartOfSpeech);
            if (lexemeResult.IsFailure)
            {
                return lexemeResult;
            }

            Lexeme lexeme = lexemeResult.Value;
            lexeme.Enrich(
                generatedLexeme.PartOfSpeech,
                entry.IpaUk,
                entry.IpaUs,
                generatedLexeme.Syllables,
                generatedLexeme.WordForms,
                generatedLexeme.CefrLevel);

            List<string> lexemeWords = [];
            foreach (GeneratedSense generatedSense in generatedLexeme.Senses)
            {
                Result<Sense> senseResult = BuildSense(lexeme.Id, lemma, generatedSense);
                if (senseResult.IsFailure)
                {
                    _logger.LogWarning("Auto-fill sense skipped: {ErrorCode}", senseResult.Error.Code);
                    continue;
                }

                senses.Add(senseResult.Value);
                lexemeWords.Add(senseResult.Value.Word);
            }

            foreach (Guid matchedId in generatedLexeme.MatchedExistingSenseIds)
            {
                if (movableById.Remove(matchedId, out Sense? sense) && sense.MoveToLexeme(lexeme.Id).IsSuccess)
                {
                    moved.Add(sense);
                    lexemeWords.Add(sense.Word);
                }
            }

            if (lexemeWords.Count == 0)
            {
                continue;
            }

            // ADR 0004: the lemma shown with lexeme data comes from a sense the caller may see.
            lexeme.RederiveLemma(lexemeWords);
            lexemes.Add(lexeme);
        }

        if (senses.Count == 0)
        {
            return Result.Failure(Error.Failure("Autofill.NoSenses", "Auto-fill produced no usable sense."));
        }

        List<MediaAsset> audio = await StoreAudioAsync(lexemes, entry, ct);

        return await _repository.SaveAsync(new AutofillSave(normalized, lexemes, senses, audio, moved), ct);
    }

    private Result<Sense> BuildSense(Guid lexemeId, string lemma, GeneratedSense generated)
    {
        Result<Sense> senseResult = Sense.CreateAutoFill(
            Guid.NewGuid(),
            lexemeId,
            lemma,
            generated.Definition,
            generated.Examples.Take(Sense.MaxExamples).ToList(),
            generated.Collocations,
            generated.Synonyms,
            generated.Antonyms,
            generated.TopicTags,
            Truncate(generated.RegisterNote, Sense.RegisterNoteMaxLength),
            generated.ChildSuitable);
        if (senseResult.IsFailure)
        {
            return senseResult;
        }

        Sense sense = senseResult.Value;
        foreach (string locale in _settings.TranslationLocales)
        {
            if (!generated.Translations.TryGetValue(locale, out string? text))
            {
                continue;
            }

            Result<SenseTranslation> translation = SenseTranslation.Create(
                Guid.NewGuid(), sense.Id, locale, Truncate(text, SenseTranslation.TextMaxLength) ?? string.Empty);
            if (translation.IsSuccess)
            {
                sense.AddTranslation(translation.Value);
            }
        }

        return Result.Success(sense);
    }

    /// <summary>Downloads each accent once and stores one named file per lexeme. A failed download
    /// only skips that audio; it never blocks the save.</summary>
    private async Task<List<MediaAsset>> StoreAudioAsync(IReadOnlyList<Lexeme> lexemes, DictionaryEntry entry, CancellationToken ct)
    {
        List<MediaAsset> assets = [];
        foreach ((string locale, string? url) in new[] { (Lexeme.UkLocale, entry.AudioUkUrl), (Lexeme.UsLocale, entry.AudioUsUrl) })
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            Result<byte[]> download = await _audioDownloader.DownloadAsync(url, ct);
            if (download.IsFailure)
            {
                _logger.LogWarning("Auto-fill audio skipped: Locale={Locale}, ErrorCode={ErrorCode}", locale, download.Error.Code);
                continue;
            }

            foreach (Lexeme lexeme in lexemes)
            {
                using MemoryStream stream = new(download.Value, writable: false);
                string path = await _fileStorage.SaveNamedAsync(stream, Lexeme.AudioFileName(lexeme.Id, locale), AudioContentType, ct);
                MediaAsset asset = new(Guid.NewGuid(), MediaAssetType.Audio, path);
                if (lexeme.AttachAudio(locale, asset.Id).IsSuccess)
                {
                    assets.Add(asset);
                }
            }
        }

        return assets;
    }

    private static string? Truncate(string? value, int maxLength) =>
        value is null ? null : value.Length > maxLength ? value[..maxLength] : value;
}
