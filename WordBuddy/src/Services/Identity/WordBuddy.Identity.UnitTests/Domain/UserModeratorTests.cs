using FluentAssertions;
using WordBuddy.Identity.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.UnitTests.Domain;

/// <summary>WB-56: the Moderator role and the admin delete guard.</summary>
public class UserModeratorTests
{
    private static User Admin() => new(Guid.NewGuid(), "admin@example.com", "Admin", "hash", AgeGroup.Adult, isAdmin: true);

    [Fact]
    public void User_SetModerator_AdultGrantSucceeds()
    {
        User user = TestData.Adult();

        Result result = user.SetModerator(true);

        result.IsSuccess.Should().BeTrue();
        user.IsModerator.Should().BeTrue();
    }

    [Fact]
    public void User_SetModerator_ChildGrantFails()
    {
        User user = TestData.Child();

        Result result = user.SetModerator(true);

        result.Error.Should().Be(UserErrors.ModeratorMustBeAdult);
        user.IsModerator.Should().BeFalse();
    }

    [Fact]
    public void User_SetModerator_AdminGrantFails()
    {
        User user = Admin();

        Result result = user.SetModerator(true);

        result.Error.Should().Be(UserErrors.ModeratorNotForAdmin);
        user.IsModerator.Should().BeFalse();
    }

    [Fact]
    public void User_SetModerator_RevokeSucceeds()
    {
        User user = TestData.Adult();
        user.SetModerator(true);

        Result result = user.SetModerator(false);

        result.IsSuccess.Should().BeTrue();
        user.IsModerator.Should().BeFalse();
    }

    [Fact]
    public void User_EnsureCanBeDeleted_AdminFails()
    {
        Admin().EnsureCanBeDeleted().Error.Should().Be(UserErrors.AdminCannotBeDeleted);
    }

    [Fact]
    public void User_EnsureCanBeDeleted_NonAdminSucceeds()
    {
        TestData.Adult().EnsureCanBeDeleted().IsSuccess.Should().BeTrue();
    }
}
