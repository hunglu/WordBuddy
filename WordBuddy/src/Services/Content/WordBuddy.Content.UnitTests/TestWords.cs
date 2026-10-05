using WordBuddy.Content.Domain;

namespace WordBuddy.Content.UnitTests;

/// <summary>Builders for <see cref="Sense"/>s in a given state. Each gets a fresh lexeme id.</summary>
internal static class TestWords
{
    public static Sense Learner(Guid ownerId, string word = "apple", string definition = "a fruit", string? example = null, AgeGroup ageGroup = AgeGroup.Adult) =>
        Sense.CreateLearner(Guid.NewGuid(), Guid.NewGuid(), ownerId, ageGroup, word, definition, example).Value;

    public static Sense Pending(Guid ownerId, string word = "apple", string definition = "a fruit", string? example = null)
    {
        Sense w = Learner(ownerId, word, definition, example);
        w.RequestShare();
        return w;
    }

    public static Sense Shared(Guid ownerId, bool visibleToChildren, string word = "apple", string definition = "a fruit", string? example = null)
    {
        Sense w = Pending(ownerId, word, definition, example);
        w.Approve(visibleToChildren, Guid.NewGuid());
        return w;
    }

    /// <summary>A shared learner word whose author deleted it, so it now belongs to the system owner.</summary>
    public static Sense Transferred(bool visibleToChildren, string word = "apple", string definition = "a fruit", string? example = null)
    {
        Sense w = Shared(Guid.NewGuid(), visibleToChildren, word, definition, example);
        w.TransferToSystem();
        return w;
    }

    public static Sense System(string word = "apple", string definition = "a fruit", string example = "I ate an apple.") =>
        Sense.CreateSystem(Guid.NewGuid(), Guid.NewGuid(), word, definition, example);
}
