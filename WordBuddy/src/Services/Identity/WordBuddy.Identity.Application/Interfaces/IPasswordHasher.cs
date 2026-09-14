namespace WordBuddy.Identity.Application.Interfaces;

/// <summary>Hashes and verifies passwords. Implemented in Infrastructure (BCrypt).</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
