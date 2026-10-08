using FluentAssertions;
using WordBuddy.Identity.Domain.SupportLinks;

namespace WordBuddy.Identity.UnitTests.Domain;

public class SupportLinkInvitationTests
{
    private readonly Guid _creator = Guid.NewGuid();
    private readonly Guid _acceptor = Guid.NewGuid();

    private SupportLinkInvitation Create(string code = "ABCD2345", string token = "token-1") =>
        SupportLinkInvitation.Create(
            Guid.NewGuid(), _creator, InvitationSide.Learner, SupportRelationship.Parent, code, token, TestData.Now, TimeSpan.FromDays(7));

    [Fact]
    public void SupportLinkInvitation_CanAccept_ExpiredFails()
    {
        SupportLinkInvitation invitation = Create();

        invitation.CanAccept(_acceptor, TestData.Now.AddDays(7)).Error.Should().Be(SupportLinkErrors.InvitationExpired);
        invitation.CanAccept(_acceptor, TestData.Now.AddDays(7).AddSeconds(-1)).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void SupportLinkInvitation_Accept_AlreadyUsedFails()
    {
        SupportLinkInvitation invitation = Create();
        invitation.Accept(_acceptor, Guid.NewGuid(), TestData.Now).IsSuccess.Should().BeTrue();

        invitation.Accept(Guid.NewGuid(), Guid.NewGuid(), TestData.Now).Error.Should().Be(SupportLinkErrors.InvitationAlreadyUsed);
        invitation.Status.Should().Be(InvitationStatus.Accepted);
    }

    [Fact]
    public void SupportLinkInvitation_CanAccept_CreatorCannotAcceptOwn()
    {
        Create().CanAccept(_creator, TestData.Now).Error.Should().Be(SupportLinkErrors.SelfLink);
    }

    [Fact]
    public void SupportLinkInvitation_HashTypedCode_MatchesStoredHashIgnoringCaseAndDash()
    {
        SupportLinkInvitation invitation = Create("ABCD2345");

        SupportLinkInvitation.HashTypedCode("abcd-2345").Should().Be(invitation.CodeHash);
        SupportLinkInvitation.HashTypedCode("ABCD2346").Should().NotBe(invitation.CodeHash);
        invitation.CodeHash.Should().NotContain("ABCD2345");
    }

    [Fact]
    public void SupportLinkInvitation_HashToken_StoresHashOnly()
    {
        SupportLinkInvitation invitation = Create(token: "secret-token");

        invitation.TokenHash.Should().Be(SupportLinkInvitation.HashToken("secret-token"));
        invitation.TokenHash.Should().NotBe("secret-token");
    }

    [Fact]
    public void SupportLinkInvitation_GenerateCode_EightCharsWithoutAmbiguousLetters()
    {
        string code = SupportLinkInvitation.GenerateCode();

        code.Should().HaveLength(SupportLinkInvitation.CodeLength);
        code.Should().NotContainAny("0", "O", "1", "I");
    }

    [Fact]
    public void SupportLinkInvitation_Cancel_OnlyCreatorAndOnlyPending()
    {
        SupportLinkInvitation invitation = Create();

        invitation.Cancel(_acceptor).Error.Should().Be(SupportLinkErrors.Forbidden);
        invitation.Cancel(_creator).IsSuccess.Should().BeTrue();
        invitation.CanAccept(_acceptor, TestData.Now).Error.Should().Be(SupportLinkErrors.InvitationAlreadyUsed);
    }
}
