using FluentAssertions;
using WordBuddy.Identity.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.UnitTests.Domain;

public class UserPreferredLanguageTests
{
    [Theory]
    [InlineData("en", "en")]
    [InlineData("vi", "vi")]
    [InlineData("VI", "vi")]
    [InlineData(" Pt-BR ", "pt-br")]
    public void User_SetPreferredLanguage_ValidCodeIsNormalisedAndStored(string input, string expected)
    {
        User user = TestData.Adult();

        Result result = user.SetPreferredLanguage(input);

        result.IsSuccess.Should().BeTrue();
        user.PreferredLanguage.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void User_SetPreferredLanguage_NullOrBlankClears(string? input)
    {
        User user = TestData.Child();
        user.SetPreferredLanguage("vi");

        user.SetPreferredLanguage(input).IsSuccess.Should().BeTrue();

        user.PreferredLanguage.Should().BeNull();
    }

    [Theory]
    [InlineData("english")]
    [InlineData("e")]
    [InlineData("en_US")]
    [InlineData("en-12345678901")]
    [InlineData("vi!")]
    public void User_SetPreferredLanguage_MalformedCodeIsRejectedAndKeepsOldValue(string input)
    {
        User user = TestData.Adult();
        user.SetPreferredLanguage("vi");

        Result result = user.SetPreferredLanguage(input);

        result.Error.Should().Be(UserErrors.InvalidLanguage);
        user.PreferredLanguage.Should().Be("vi");
    }
}
