using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Interfaces.Autofill;

/// <summary>Persistence of the auto-fill catalog and the child approval queues.</summary>
public interface IAutofillRepository
{
    /// <summary>Returns the <see cref="SenseOrigin.AutoFill"/> senses of a normalized lemma, with lexeme,
    /// lexeme audio and translations loaded. Empty when the word is not in the catalog yet.</summary>
    Task<Result<IReadOnlyList<Sense>>> GetCatalogAsync(string normalizedLemma, CancellationToken ct = default);

    /// <summary>Returns tracked System or Shared senses on the null-part-of-speech lexeme of the
    /// lemma. Private learner senses are never returned.</summary>
    Task<Result<IReadOnlyList<Sense>>> GetMovableCatalogSensesAsync(string normalizedLemma, CancellationToken ct = default);

    /// <summary>Saves new lexemes, their senses, translations and audio assets, plus moved senses,
    /// in one transaction, then deletes the null-part-of-speech lexeme of the lemma when no sense
    /// references it any more. Returns <see cref="ErrorType.Conflict"/> when a concurrent save
    /// stored the same (lemma, part of speech) first — the caller re-reads the catalog.</summary>
    Task<Result> SaveAsync(AutofillSave save, CancellationToken ct = default);

    /// <summary>Returns a learner's links that wait for child approval (sense not globally
    /// approved, link not approved), senses loaded, oldest first.</summary>
    Task<Result<IReadOnlyList<LearnerWord>>> GetPendingLinksForLearnerAsync(Guid learnerId, CancellationToken ct = default);

    /// <summary>Returns auto-filled senses that are not <see cref="Sense.VisibleToChildren"/> and that
    /// at least one child linked without a supporter approval yet, oldest first.</summary>
    Task<Result<IReadOnlyList<Sense>>> GetPendingSensesForAdminAsync(CancellationToken ct = default);

    /// <summary>Returns the tracked link of a learner to a sense, sense loaded, or not found.</summary>
    Task<Result<LearnerWord>> GetTrackedLinkAsync(Guid learnerId, Guid senseId, CancellationToken ct = default);

    /// <summary>Returns a tracked sense with its lexeme loaded, or not found.</summary>
    Task<Result<Sense>> GetTrackedSenseAsync(Guid senseId, CancellationToken ct = default);

    /// <summary>Saves changes to tracked entities.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>Everything one auto-fill run stores.</summary>
/// <param name="NormalizedLemma">Lemma whose null-part-of-speech lexeme may become orphaned.</param>
public sealed record AutofillSave(
    string NormalizedLemma,
    IReadOnlyList<Lexeme> Lexemes,
    IReadOnlyList<Sense> Senses,
    IReadOnlyList<MediaAsset> AudioAssets,
    IReadOnlyList<Sense> MovedSenses);
