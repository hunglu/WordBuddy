using System.Net.Mail;

namespace WordBuddy.Identity.Infrastructure.Seeding;

/// <summary>
/// Settings for the one-time first admin account (<c>Bootstrap:Admin</c>). Used only by the
/// <c>migrate</c> run mode. The password is never logged.
/// </summary>
public sealed class AdminBootstrapOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Bootstrap:Admin";

    /// <summary>Minimum accepted password length.</summary>
    public const int PasswordMinLength = 12;

    /// <summary>Gets or sets the admin email.</summary>
    public string? Email { get; set; }

    /// <summary>Gets or sets the admin password.</summary>
    public string? Password { get; set; }

    /// <summary>Gets or sets the admin display name.</summary>
    public string DisplayName { get; set; } = "Admin";

    /// <summary>Returns true when email, password and display name are usable.</summary>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(Email) || !MailAddress.TryCreate(Email, out MailAddress? address) || address.Address != Email)
        {
            return false;
        }

        if (string.IsNullOrEmpty(Password) || Password.Length < PasswordMinLength)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(DisplayName);
    }
}
