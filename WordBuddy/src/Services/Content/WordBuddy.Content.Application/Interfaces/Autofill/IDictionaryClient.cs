using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Interfaces.Autofill;

/// <summary>Looks a word up in an external dictionary (IPA, audio, parts of speech). Only the
/// typed word is sent. Failures come back as a failed <see cref="Result"/>, never an exception:
/// <see cref="ErrorType.NotFound"/> when the dictionary does not know the word, a failure otherwise.</summary>
public interface IDictionaryClient
{
    /// <summary>Looks up <paramref name="word"/>.</summary>
    Task<Result<DictionaryEntry>> LookupAsync(string word, CancellationToken ct = default);
}
