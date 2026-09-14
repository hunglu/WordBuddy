namespace WordBuddy.Shared.Kernel;

/// <summary>Base type for a domain entity identified by a <see cref="Guid"/>. Equality is by identity, not by value.</summary>
public abstract class Entity : IEquatable<Entity>
{
    /// <summary>Gets the entity's unique identifier.</summary>
    public Guid Id { get; }

    protected Entity(Guid id)
    {
        Id = id;
    }

    public bool Equals(Entity? other) =>
        other is not null && (ReferenceEquals(this, other) || (GetType() == other.GetType() && Id == other.Id));

    public override bool Equals(object? obj) => Equals(obj as Entity);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? left, Entity? right) => Equals(left, right);

    public static bool operator !=(Entity? left, Entity? right) => !Equals(left, right);
}
