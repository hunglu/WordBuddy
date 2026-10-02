using WordBuddy.Content.Domain;

namespace WordBuddy.Content.UnitTests;

/// <summary>Builders for <see cref="VocabularyWord"/>s in a given state.</summary>
internal static class TestWords
{
    public static VocabularyWord Learner(Guid ownerId, string word = "apple", string definition = "a fruit", string? example = null, AgeGroup ageGroup = AgeGroup.Adult) =>
        VocabularyWord.CreateLearner(Guid.NewGuid(), ownerId, ageGroup, word, definition, example).Value;

    public static VocabularyWord Pending(Guid ownerId, string word = "apple", string definition = "a fruit", string? example = null)
    {
        VocabularyWord w = Learner(ownerId, word, definition, example);
        w.RequestShare();
        return w;
    }

    public static VocabularyWord Shared(Guid ownerId, bool visibleToChildren, string word = "apple", string definition = "a fruit", string? example = null)
    {
        VocabularyWord w = Pending(ownerId, word, definition, example);
        w.Approve(visibleToChildren, Guid.NewGuid());
        return w;
    }

    /// <summary>A shared learner word whose author deleted it, so it now belongs to the system owner.</summary>
    public static VocabularyWord Transferred(bool visibleToChildren, string word = "apple", string definition = "a fruit", string? example = null)
    {
        VocabularyWord w = Shared(Guid.NewGuid(), visibleToChildren, word, definition, example);
        w.TransferToSystem();
        return w;
    }

    public static VocabularyWord System(string word = "apple", string definition = "a fruit", string example = "I ate an apple.") =>
        VocabularyWord.CreateSystem(Guid.NewGuid(), word, definition, example);
}
