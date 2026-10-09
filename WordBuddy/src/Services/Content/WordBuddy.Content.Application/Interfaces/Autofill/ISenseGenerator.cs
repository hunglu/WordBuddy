using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Interfaces.Autofill;

/// <summary>Generates catalog senses for a word (definition, examples, translations, enrichment).
/// Sends only the word, its parts of speech and existing <b>catalog</b> definitions — never private
/// learner text. Failures (timeout, bad JSON) come back as a failed <see cref="Result"/>.</summary>
public interface ISenseGenerator
{
    /// <summary>Generates senses for <paramref name="request"/>.</summary>
    Task<Result<GeneratedWord>> GenerateAsync(SenseGenerationRequest request, CancellationToken ct = default);
}
