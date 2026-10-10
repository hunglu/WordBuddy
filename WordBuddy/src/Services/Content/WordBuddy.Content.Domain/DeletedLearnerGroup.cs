namespace WordBuddy.Content.Domain;

/// <summary>
/// Tombstone of a deleted Identity group (id and time only). Member events at or before
/// <see cref="DeletedAtUtc"/> are ignored, so a delete that arrives before an older member event
/// cannot leave an active member in a deleted group.
/// </summary>
public sealed class DeletedLearnerGroup
{
    /// <summary>Gets the Identity group id.</summary>
    public Guid GroupId { get; private set; }

    /// <summary>Gets the time of the delete event, UTC.</summary>
    public DateTime DeletedAtUtc { get; private set; }

    private DeletedLearnerGroup()
    {
    }

    /// <summary>Creates the tombstone from the first delete event seen.</summary>
    public static DeletedLearnerGroup Create(Guid groupId, DateTime deletedAtUtc) =>
        new() { GroupId = groupId, DeletedAtUtc = deletedAtUtc };

    /// <summary>Moves the delete time forward. Returns <see langword="false"/> when the event is not newer.</summary>
    public bool MarkDeleted(DateTime deletedAtUtc)
    {
        if (deletedAtUtc <= DeletedAtUtc)
        {
            return false;
        }

        DeletedAtUtc = deletedAtUtc;
        return true;
    }
}
