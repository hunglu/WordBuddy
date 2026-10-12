using FluentAssertions;
using WordBuddy.Identity.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.UnitTests.Domain;

public class UserTimeZoneGuardianTests
{
    [Theory]
    [InlineData("Asia/Ho_Chi_Minh")]
    [InlineData("America/New_York")]
    [InlineData(" UTC ")]
    public void User_SetTimeZone_KnownIdIsStored(string id)
    {
        User user = TestData.Adult();

        Result result = user.SetTimeZone(id);

        result.IsSuccess.Should().BeTrue();
        user.TimeZoneId.Should().Be(id.Trim());
    }

    [Theory]
    [InlineData("Mars/Olympus_Mons")]
    [InlineData("not a zone")]
    public void User_SetTimeZone_UnknownIdIsRejectedAndKeepsOldValue(string id)
    {
        User user = TestData.Adult();
        user.SetTimeZone("Asia/Ho_Chi_Minh");

        Result result = user.SetTimeZone(id);

        result.Error.Should().Be(UserErrors.UnknownTimeZone);
        user.TimeZoneId.Should().Be("Asia/Ho_Chi_Minh");
    }

    [Fact]
    public void User_SetTimeZone_TooLongIdIsRejected()
    {
        User user = TestData.Adult();

        user.SetTimeZone(new string('A', User.TimeZoneIdMaxLength + 1)).Error.Should().Be(UserErrors.UnknownTimeZone);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void User_SetTimeZone_NullOrBlankClears(string? id)
    {
        User user = TestData.Adult();
        user.SetTimeZone("Asia/Ho_Chi_Minh");

        user.SetTimeZone(id).IsSuccess.Should().BeTrue();

        user.TimeZoneId.Should().BeNull();
    }

    [Fact]
    public void User_SetGuardian_AdultCanBeGrantedAndRemoved()
    {
        User adult = TestData.Adult();

        adult.SetGuardian(true).IsSuccess.Should().BeTrue();
        adult.IsGuardian.Should().BeTrue();

        adult.SetGuardian(false).IsSuccess.Should().BeTrue();
        adult.IsGuardian.Should().BeFalse();
    }

    [Fact]
    public void User_SetGuardian_ChildCannotBeGranted()
    {
        User child = TestData.Child();

        Result result = child.SetGuardian(true);

        result.Error.Should().Be(UserErrors.GuardianMustBeAdult);
        child.IsGuardian.Should().BeFalse();
    }

    [Fact]
    public void User_SetGuardian_RemovingFromChildIsHarmless()
    {
        TestData.Child().SetGuardian(false).IsSuccess.Should().BeTrue();
    }
}
