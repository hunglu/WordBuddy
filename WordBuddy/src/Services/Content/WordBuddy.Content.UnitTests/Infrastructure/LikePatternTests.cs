using FluentAssertions;
using WordBuddy.Content.Infrastructure.Persistence;

namespace WordBuddy.Content.UnitTests.Infrastructure;

public class LikePatternTests
{
    [Theory]
    [InlineData("50%", "50\\%")]
    [InlineData("a_b", "a\\_b")]
    [InlineData("a[b", "a\\[b")]
    [InlineData("%_[", "\\%\\_\\[")]
    public void LikePattern_Escape_EscapesPercentUnderscoreBracket(string input, string expected)
    {
        LikePattern.Escape(input).Should().Be(expected);
    }

    [Fact]
    public void LikePattern_Escape_EscapesBackslashFirst()
    {
        // "\%" must become "\\" + "\%", not a double escape of the escape added for "%".
        LikePattern.Escape("\\%").Should().Be("\\\\\\%");
        LikePattern.Escape("\\").Should().Be("\\\\");
    }

    [Fact]
    public void LikePattern_Escape_PlainText_Unchanged()
    {
        LikePattern.Escape("APPLE PIE").Should().Be("APPLE PIE");
    }
}
