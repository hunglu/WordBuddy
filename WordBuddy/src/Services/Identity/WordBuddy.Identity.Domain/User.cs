using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Domain;

/// <summary>A learner or admin account.</summary>
public sealed class User : Entity
{
    /// <summary>Gets the account's email address, used to sign in.</summary>
    public string Email { get; }

    /// <summary>Gets the display name shown in the UI.</summary>
    public string DisplayName { get; }

    /// <summary>Gets the salted password hash. Never exposed outside <c>Identity.Infrastructure</c>.</summary>
    public string PasswordHash { get; }

    /// <summary>Gets the account's age group, used to enforce content restrictions.</summary>
    public AgeGroup AgeGroup { get; }

    /// <summary>Gets a value indicating whether this account has administrative privileges.</summary>
    public bool IsAdmin { get; }

    public User(Guid id, string email, string displayName, string passwordHash, AgeGroup ageGroup, bool isAdmin)
        : base(id)
    {
        Email = email;
        DisplayName = displayName;
        PasswordHash = passwordHash;
        AgeGroup = ageGroup;
        IsAdmin = isAdmin;
    }
}
