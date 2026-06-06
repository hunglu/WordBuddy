using WordBuddy.Domain.Enums;

namespace WordBuddy.Domain.Entities;

/// <summary>A learner or admin account with profile and role information.</summary>
public sealed class User
{
    /// <summary>Initializes a new <see cref="User"/>.</summary>
    /// <param name="id">Unique identifier.</param>
    /// <param name="email">Email address used for authentication.</param>
    /// <param name="passwordHash">Bcrypt-hashed password. Never expose in API responses.</param>
    /// <param name="displayName">Name shown in the application UI.</param>
    /// <param name="ageGroup">Age group classification.</param>
    /// <param name="level">Current English proficiency level.</param>
    /// <param name="createdAt">UTC timestamp of account creation.</param>
    /// <param name="updatedAt">UTC timestamp of the most recent update.</param>
    public User(
        Guid id,
        string email,
        string passwordHash,
        string displayName,
        AgeGroup ageGroup,
        Level level,
        DateTime createdAt,
        DateTime updatedAt)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        DisplayName = displayName;
        AgeGroup = ageGroup;
        Level = level;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the email address used for authentication.</summary>
    public string Email { get; }

    /// <summary>Gets the bcrypt-hashed password.</summary>
    public string PasswordHash { get; }

    /// <summary>Gets the display name shown in the application UI.</summary>
    public string DisplayName { get; }

    /// <summary>Gets the age group classification.</summary>
    public AgeGroup AgeGroup { get; }

    /// <summary>Gets the current English proficiency level.</summary>
    public Level Level { get; }

    /// <summary>Gets the UTC timestamp when the account was created.</summary>
    public DateTime CreatedAt { get; }

    /// <summary>Gets the UTC timestamp when the account was last updated.</summary>
    public DateTime UpdatedAt { get; }
}
