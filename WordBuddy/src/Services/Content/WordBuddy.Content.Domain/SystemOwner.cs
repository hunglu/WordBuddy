namespace WordBuddy.Content.Domain;

/// <summary>The well-known owner of every <see cref="VocabularySource.System"/> word. A plain
/// value defined only in Content — not a row in Identity and never a foreign key. Identity issues
/// <c>Guid.NewGuid()</c> user ids, so no JWT can carry this id; <see cref="Sense.CreateLearner"/>
/// still rejects it as a guard.</summary>
public static class SystemOwner
{
    /// <summary>The system owner's user id: <c>00000000-0000-0000-0000-00000000c0de</c>.</summary>
    public static readonly Guid UserId = new("00000000-0000-0000-0000-00000000c0de");
}
