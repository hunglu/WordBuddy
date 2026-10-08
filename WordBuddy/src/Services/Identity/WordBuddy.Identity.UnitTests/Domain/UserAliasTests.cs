using FluentAssertions;
using WordBuddy.Identity.Domain;

namespace WordBuddy.Identity.UnitTests.Domain;

public class UserAliasTests
{
    [Theory]
    [InlineData("Kid")]
    [InlineData("kid")]
    public void User_SetAliasAndAvatar_ChildAliasEqualToDisplayNameRejected(string alias)
    {
        User child = TestData.Child("Kid");

        child.SetAliasAndAvatar(alias, null).Error.Should().Be(UserErrors.AliasRevealsIdentity);
    }

    [Fact]
    public void User_SetAliasAndAvatar_ChildAliasEqualToEmailLocalPartRejected()
    {
        User child = new(Guid.NewGuid(), "mila.k@example.com", "Mila", "hash", AgeGroup.Child, isAdmin: false);

        child.SetAliasAndAvatar("Mila.K", null).Error.Should().Be(UserErrors.AliasRevealsIdentity);
    }

    [Fact]
    public void User_SetAliasAndAvatar_AdultMayUseDisplayName()
    {
        User adult = TestData.Adult("Sam");

        adult.SetAliasAndAvatar("Sam", "owl").IsSuccess.Should().BeTrue();
        adult.Alias.Should().Be("Sam");
        adult.AvatarId.Should().Be("owl");
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("abcdefghijklmnopqrstu")]
    public void User_SetAliasAndAvatar_LengthOutOfRangeRejected(string alias)
    {
        TestData.Adult().SetAliasAndAvatar(alias, null).Error.Should().Be(UserErrors.AliasLength);
    }

    [Fact]
    public void User_SetAliasAndAvatar_UnknownAvatarRejected()
    {
        TestData.Adult().SetAliasAndAvatar(null, "unicorn").Error.Should().Be(UserErrors.UnknownAvatar);
    }

    [Fact]
    public void User_PublicName_ChildWithoutAliasIsNeutral()
    {
        User child = TestData.Child("Kid");

        child.PublicName.Should().Be("Child learner");
        child.SetAliasAndAvatar("RocketFox", "fox");
        child.PublicName.Should().Be("RocketFox");
    }
}
