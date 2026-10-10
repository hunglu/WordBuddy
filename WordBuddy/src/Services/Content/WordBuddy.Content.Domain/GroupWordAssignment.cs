using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Domain;

/// <summary>History row: a supporter assigned a sense to a group on a date. New members do not
/// receive past assignments; the owner can assign again.</summary>
public sealed class GroupWordAssignment : Entity
{
    /// <summary>Gets the Identity group id.</summary>
    public Guid GroupId { get; private set; }

    /// <summary>Gets the assigned sense.</summary>
    public Guid SenseId { get; private set; }

    /// <summary>Gets the supporter who assigned the sense.</summary>
    public Guid AssignedBy { get; private set; }

    /// <summary>Gets when the sense was assigned, UTC.</summary>
    public DateTime AssignedAtUtc { get; private set; }

    /// <summary>Creates an assignment row.</summary>
    public GroupWordAssignment(Guid id, Guid groupId, Guid senseId, Guid assignedBy, DateTime assignedAtUtc)
        : base(id)
    {
        GroupId = groupId;
        SenseId = senseId;
        AssignedBy = assignedBy;
        AssignedAtUtc = assignedAtUtc;
    }
}
