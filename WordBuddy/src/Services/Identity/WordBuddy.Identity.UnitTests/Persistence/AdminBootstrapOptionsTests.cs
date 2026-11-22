using FluentAssertions;
using WordBuddy.Identity.Infrastructure.Seeding;

namespace WordBuddy.Identity.UnitTests.Persistence;

public sealed class AdminBootstrapOptionsTests
{
    [Fact]
    public void AdminBootstrapOptions_IsValid_FalseWhenEmailMissing()
    {
        AdminBootstrapOptions options = new() { Email = null, Password = "a-long-first-password" };

        options.IsValid().Should().BeFalse();
    }

    [Fact]
    public void AdminBootstrapOptions_IsValid_FalseWhenEmailInvalid()
    {
        AdminBootstrapOptions options = new() { Email = "not-an-email", Password = "a-long-first-password" };

        options.IsValid().Should().BeFalse();
    }

    [Fact]
    public void AdminBootstrapOptions_IsValid_FalseWhenPasswordShort()
    {
        AdminBootstrapOptions options = new() { Email = "admin@example.com", Password = "short" };

        options.IsValid().Should().BeFalse();
    }

    [Fact]
    public void AdminBootstrapOptions_IsValid_TrueWhenEmailAndPasswordValid()
    {
        AdminBootstrapOptions options = new() { Email = "admin@example.com", Password = "a-long-first-password" };

        options.IsValid().Should().BeTrue();
    }
}
