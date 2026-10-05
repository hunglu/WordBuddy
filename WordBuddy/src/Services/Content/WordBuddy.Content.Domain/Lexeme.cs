using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Domain;

/// <summary>A dictionary headword: one spelling plus one part of speech (ADR 0004). Groups the
/// <see cref="Sense"/>s that share it. Has no visibility of its own and is never returned by an
/// endpoint directly. <see cref="PartOfSpeech"/> is <see langword="null"/> while not known yet.</summary>
public sealed class Lexeme : Entity
{
    /// <summary>Maximum length of <see cref="Lemma"/> and <see cref="NormalizedLemma"/>.</summary>
    public const int LemmaMaxLength = 200;

    /// <summary>The headword as first saved, trimmed of spaces.</summary>
    public string Lemma { get; private set; }

    /// <summary><see cref="Sense.NormalizeWord"/> of <see cref="Lemma"/> — the lookup key, together
    /// with <see cref="PartOfSpeech"/>.</summary>
    public string NormalizedLemma { get; private set; }

    /// <summary>Part of speech; <see langword="null"/> while not known yet.</summary>
    public PartOfSpeech? PartOfSpeech { get; private set; }

    /// <summary>British IPA transcription.</summary>
    public string? IpaUk { get; private set; }

    /// <summary>American IPA transcription.</summary>
    public string? IpaUs { get; private set; }

    /// <summary>British pronunciation audio (<c>lexeme-{id}-en-GB.mp3</c>).</summary>
    public Guid? UkAudioAssetId { get; private set; }
    /// <summary>Navigation to the British audio asset.</summary>
    public MediaAsset? UkAudio { get; private set; }

    /// <summary>American pronunciation audio (<c>lexeme-{id}-en-US.mp3</c>).</summary>
    public Guid? UsAudioAssetId { get; private set; }
    /// <summary>Navigation to the American audio asset.</summary>
    public MediaAsset? UsAudio { get; private set; }

    /// <summary>Syllable split, for example <c>ap·ple</c>.</summary>
    public string? Syllables { get; private set; }

    /// <summary>Inflected forms (plural, past tense, ...), stored as a JSON list.</summary>
    public IReadOnlyList<string> WordForms { get; private set; }

    /// <summary>CEFR level of the headword.</summary>
    public CefrLevel? CefrLevel { get; private set; }

    /// <summary>Rank in a frequency list; lower is more common.</summary>
    public int? FrequencyRank { get; private set; }

    /// <summary>When the lexeme was created (UTC).</summary>
    public DateTime CreatedAtUtc { get; private set; }

    private Lexeme(Guid id, string lemma, string normalizedLemma, PartOfSpeech? partOfSpeech, DateTime createdAtUtc)
        : base(id)
    {
        Lemma = lemma;
        NormalizedLemma = normalizedLemma;
        PartOfSpeech = partOfSpeech;
        WordForms = [];
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Creates a lexeme from <paramref name="lemma"/>: trims spaces, normalizes it with
    /// <see cref="Sense.NormalizeWord"/>, and stores the optional part of speech. Fails on an empty
    /// or too long lemma.</summary>
    public static Result<Lexeme> Create(Guid id, string lemma, PartOfSpeech? partOfSpeech = null)
    {
        string trimmed = (lemma ?? string.Empty).Trim(' ');
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return Result.Failure<Lexeme>(Error.Validation("Lexeme.EmptyLemma", "A lexeme needs a non-empty lemma."));
        }

        if (trimmed.Length > LemmaMaxLength)
        {
            return Result.Failure<Lexeme>(Error.Validation(
                "Lexeme.LemmaTooLong",
                $"A lemma may have at most {LemmaMaxLength} characters."));
        }

        return Result.Success(new Lexeme(id, trimmed, Sense.NormalizeWord(trimmed), partOfSpeech, DateTime.UtcNow));
    }
}
