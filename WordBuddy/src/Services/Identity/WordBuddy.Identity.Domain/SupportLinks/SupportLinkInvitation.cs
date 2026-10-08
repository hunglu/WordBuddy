using System.Security.Cryptography;
using System.Text;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Domain.SupportLinks;

/// <summary>
/// An open offer to create a link, shared as an 8-character code or a token link. Only hashes of
/// the code and token are stored. Expires after <see cref="ExpiresAtUtc"/>.
/// </summary>
public sealed class SupportLinkInvitation : Entity
{
    /// <summary>Length of the human-typed code.</summary>
    public const int CodeLength = 8;

    // No 0/O/1/I to keep codes easy to type for children.
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    /// <summary>Gets the user who created the invitation.</summary>
    public Guid CreatedById { get; private set; }

    /// <summary>Gets which side of the future link the creator is.</summary>
    public InvitationSide CreatorSide { get; private set; }

    /// <summary>Gets the optional relationship label for the future link.</summary>
    public SupportRelationship? Relationship { get; private set; }

    /// <summary>Gets the SHA-256 hash of the code.</summary>
    public string CodeHash { get; private set; } = string.Empty;

    /// <summary>Gets the SHA-256 hash of the token.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>Gets the current status.</summary>
    public InvitationStatus Status { get; private set; }

    /// <summary>Gets when the invitation was created, UTC.</summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Gets when the invitation expires, UTC.</summary>
    public DateTime ExpiresAtUtc { get; private set; }

    /// <summary>Gets the user who accepted it, if any.</summary>
    public Guid? AcceptedById { get; private set; }

    /// <summary>Gets the link created on accept, if any.</summary>
    public Guid? LinkId { get; private set; }

    private SupportLinkInvitation(Guid id) : base(id)
    {
    }

    /// <summary>Creates an invitation from the given plain code and token (hashed here).</summary>
    public static SupportLinkInvitation Create(
        Guid id,
        Guid createdById,
        InvitationSide creatorSide,
        SupportRelationship? relationship,
        string code,
        string token,
        DateTime nowUtc,
        TimeSpan lifetime) =>
        new(id)
        {
            CreatedById = createdById,
            CreatorSide = creatorSide,
            Relationship = relationship,
            CodeHash = Hash(NormalizeCode(code)),
            TokenHash = Hash(token),
            Status = InvitationStatus.Pending,
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = nowUtc.Add(lifetime),
        };

    /// <summary>Gets a value indicating whether the invitation is expired at <paramref name="nowUtc"/>.</summary>
    public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresAtUtc;

    /// <summary>Checks that <paramref name="acceptorId"/> may accept now. Does not change state.</summary>
    public Result CanAccept(Guid acceptorId, DateTime nowUtc)
    {
        if (Status != InvitationStatus.Pending)
        {
            return Result.Failure(SupportLinkErrors.InvitationAlreadyUsed);
        }

        if (IsExpired(nowUtc))
        {
            return Result.Failure(SupportLinkErrors.InvitationExpired);
        }

        return acceptorId == CreatedById ? Result.Failure(SupportLinkErrors.SelfLink) : Result.Success();
    }

    /// <summary>Marks the invitation accepted and records the created link.</summary>
    public Result Accept(Guid acceptorId, Guid linkId, DateTime nowUtc)
    {
        Result check = CanAccept(acceptorId, nowUtc);
        if (check.IsFailure)
        {
            return check;
        }

        Status = InvitationStatus.Accepted;
        AcceptedById = acceptorId;
        LinkId = linkId;
        return Result.Success();
    }

    /// <summary>Cancels a pending invitation. Creator only.</summary>
    public Result Cancel(Guid actorId)
    {
        if (actorId != CreatedById)
        {
            return Result.Failure(SupportLinkErrors.Forbidden);
        }

        if (Status != InvitationStatus.Pending)
        {
            return Result.Failure(SupportLinkErrors.InvitationAlreadyUsed);
        }

        Status = InvitationStatus.Cancelled;
        return Result.Success();
    }

    /// <summary>Generates a random 8-character code.</summary>
    public static string GenerateCode()
    {
        StringBuilder builder = new(CodeLength);
        for (int i = 0; i < CodeLength; i++)
        {
            builder.Append(CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)]);
        }

        return builder.ToString();
    }

    /// <summary>Generates a random URL-safe token (32 bytes).</summary>
    public static string GenerateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>Hashes a typed code the same way it is stored.</summary>
    public static string HashTypedCode(string code) => Hash(NormalizeCode(code));

    /// <summary>Hashes a token the same way it is stored.</summary>
    public static string HashToken(string token) => Hash(token);

    private static string NormalizeCode(string code) => code.Trim().Replace("-", string.Empty).ToUpperInvariant();

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
