using FluentAssertions;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Domain;

public class SenseTranslationTests
{
    [Theory]
    [InlineData("vi", "vi")]
    [InlineData("vi-VN", "vi-VN")]
    [InlineData("vi-vn", "vi-VN")]
    [InlineData("VI-vn", "vi-VN")]
    [InlineData("es-419", "es-419")]
    public void SenseTranslation_Create_AcceptsAndNormalizesValidLocale(string locale, string expected)
    {
        Result<SenseTranslation> result = SenseTranslation.Create(Guid.NewGuid(), Guid.NewGuid(), locale, "quả táo");

        result.IsSuccess.Should().BeTrue();
        result.Value.Locale.Should().Be(expected);
        result.Value.Text.Should().Be("quả táo");
    }

    [Theory]
    [InlineData("")]
    [InlineData("vietnamese")]
    [InlineData("vi_VN")]
    public void SenseTranslation_Create_FailsOnInvalidLocale(string locale)
    {
        Result<SenseTranslation> result = SenseTranslation.Create(Guid.NewGuid(), Guid.NewGuid(), locale, "quả táo");

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SenseTranslation_Create_FailsOnEmptyText(string text)
    {
        Result<SenseTranslation> result = SenseTranslation.Create(Guid.NewGuid(), Guid.NewGuid(), "vi", text);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void SenseTranslation_Create_FailsOnTextOver500Characters()
    {
        Result<SenseTranslation> result = SenseTranslation.Create(Guid.NewGuid(), Guid.NewGuid(), "vi", new string('a', 501));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void SenseTranslation_Create_AcceptsTextOf500Characters()
    {
        Result<SenseTranslation> result = SenseTranslation.Create(Guid.NewGuid(), Guid.NewGuid(), "vi", new string('a', 500));

        result.IsSuccess.Should().BeTrue();
    }
}
