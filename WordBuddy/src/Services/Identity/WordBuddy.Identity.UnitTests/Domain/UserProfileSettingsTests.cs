using FluentAssertions;
using WordBuddy.Identity.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.UnitTests.Domain;

/// <summary>WB-20: date of birth, age band, display name and password rules of <see cref="User"/>.</summary>
public class UserProfileSettingsTests
{
    private static readonly DateOnly Today = new(2026, 10, 13);

    [Fact]
    public void User_SetDateOfBirth_FutureDateRejected()
    {
        TestData.Adult().SetDateOfBirth(Today.AddDays(1), Today).Error.Should().Be(UserErrors.DateOfBirthInvalid);
    }

    [Fact]
    public void User_SetDateOfBirth_OlderThan120Rejected()
    {
        TestData.Adult().SetDateOfBirth(Today.AddYears(-121), Today).Error.Should().Be(UserErrors.DateOfBirthInvalid);
    }

    [Fact]
    public void User_SetDateOfBirth_UnderThreeRejected()
    {
        TestData.Child().SetDateOfBirth(Today.AddYears(-2), Today).Error.Should().Be(UserErrors.DateOfBirthInvalid);
    }

    [Fact]
    public void User_SetDateOfBirth_ChildAged18Rejected()
    {
        Result result = TestData.Child().SetDateOfBirth(Today.AddYears(-18), Today);

        result.Error.Should().Be(UserErrors.DateOfBirthAgeGroupMismatch);
    }

    [Fact]
    public void User_SetDateOfBirth_AdultUnder13Rejected()
    {
        User adult = TestData.Adult();

        adult.SetDateOfBirth(Today.AddYears(-12), Today).Error.Should().Be(UserErrors.DateOfBirthAgeGroupMismatch);
        adult.DateOfBirth.Should().BeNull();
    }

    [Fact]
    public void User_SetDateOfBirth_ValidChildStored()
    {
        User child = TestData.Child();
        DateOnly dob = Today.AddYears(-8);

        child.SetDateOfBirth(dob, Today).IsSuccess.Should().BeTrue();
        child.DateOfBirth.Should().Be(dob);
    }

    [Fact]
    public void User_SetDateOfBirth_ValidAdultStored()
    {
        User adult = TestData.Adult();
        DateOnly dob = Today.AddYears(-30);

        adult.SetDateOfBirth(dob, Today).IsSuccess.Should().BeTrue();
        adult.DateOfBirth.Should().Be(dob);
    }

    [Fact]
    public void User_SetDateOfBirth_NullClears()
    {
        User adult = TestData.Adult();
        adult.SetDateOfBirth(Today.AddYears(-30), Today);

        adult.SetDateOfBirth(null, Today).IsSuccess.Should().BeTrue();
        adult.DateOfBirth.Should().BeNull();
    }

    [Theory]
    [InlineData(6, AgeBand.Under7)]
    [InlineData(7, AgeBand.Age7To9)]
    [InlineData(9, AgeBand.Age7To9)]
    [InlineData(10, AgeBand.Age10To12)]
    [InlineData(12, AgeBand.Age10To12)]
    [InlineData(13, AgeBand.Age13To17)]
    [InlineData(17, AgeBand.Age13To17)]
    public void User_GetAgeBand_ChildBoundariesMapToBand(int years, AgeBand expected)
    {
        User child = TestData.Child();
        child.SetDateOfBirth(Today.AddYears(-years), Today).IsSuccess.Should().BeTrue();

        child.GetAgeBand(Today).Should().Be(expected);
    }

    [Theory]
    [InlineData(13, AgeBand.Age13To17)]
    [InlineData(17, AgeBand.Age13To17)]
    [InlineData(18, AgeBand.Adult)]
    [InlineData(60, AgeBand.Adult)]
    public void User_GetAgeBand_AdultBoundariesMapToBand(int years, AgeBand expected)
    {
        User adult = TestData.Adult();
        adult.SetDateOfBirth(Today.AddYears(-years), Today).IsSuccess.Should().BeTrue();

        adult.GetAgeBand(Today).Should().Be(expected);
    }

    [Fact]
    public void User_GetAgeBand_DayBeforeBirthdayStaysInLowerBand()
    {
        User child = TestData.Child();
        child.SetDateOfBirth(Today.AddYears(-7).AddDays(1), Today).IsSuccess.Should().BeTrue(); // turns 7 tomorrow

        child.GetAgeBand(Today).Should().Be(AgeBand.Under7);
        child.GetAgeBand(Today.AddDays(1)).Should().Be(AgeBand.Age7To9);
    }

    [Fact]
    public void User_GetAgeBand_BirthdayTodayMovesToNextBand()
    {
        User child = TestData.Child();
        child.SetDateOfBirth(Today.AddYears(-10), Today).IsSuccess.Should().BeTrue();

        child.GetAgeBand(Today).Should().Be(AgeBand.Age10To12);
    }

    [Fact]
    public void User_GetAgeBand_NoDateOfBirthIsNull()
    {
        TestData.Adult().GetAgeBand(Today).Should().BeNull();
    }

    [Theory]
    [InlineData("A")]
    [InlineData(" B ")]
    [InlineData("")]
    public void User_SetDisplayName_TooShortRejected(string name)
    {
        TestData.Adult().SetDisplayName(name).Error.Should().Be(UserErrors.DisplayNameLength);
    }

    [Fact]
    public void User_SetDisplayName_TooLongRejected()
    {
        TestData.Adult().SetDisplayName(new string('x', 51)).Error.Should().Be(UserErrors.DisplayNameLength);
    }

    [Fact]
    public void User_SetDisplayName_TrimmedAndStored()
    {
        User child = TestData.Child();

        child.SetDisplayName("  Mila R  ").IsSuccess.Should().BeTrue();
        child.DisplayName.Should().Be("Mila R");
    }

    [Fact]
    public void User_ChangePassword_StoresNewHash()
    {
        User user = TestData.Adult();

        user.ChangePassword("new-hash").IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be("new-hash");
    }

    [Fact]
    public void User_ChangePassword_EmptyHashRejected()
    {
        User user = TestData.Adult();

        user.ChangePassword(" ").Error.Should().Be(UserErrors.PasswordTooWeak);
        user.PasswordHash.Should().Be("hash");
    }
}
