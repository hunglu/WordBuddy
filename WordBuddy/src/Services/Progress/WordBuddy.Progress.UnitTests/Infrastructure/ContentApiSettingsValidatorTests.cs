using FluentAssertions;
using Microsoft.Extensions.Options;
using WordBuddy.Progress.Infrastructure.Settings;

namespace WordBuddy.Progress.UnitTests.Infrastructure;

public class ContentApiSettingsValidatorTests
{
    private readonly ContentApiSettingsValidator _validator = new();

    [Fact]
    public void ContentApiSettingsValidator_Validate_Defaults_Succeeds()
    {
        ValidateOptionsResult result = _validator.Validate(null, new ContentApiSettings());

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("00:00:00")]
    [InlineData("-00:00:05")]
    public void ContentApiSettingsValidator_Validate_NonPositivePollInterval_FailsWithClearMessage(string interval)
    {
        ContentApiSettings settings = new() { RemapPollInterval = TimeSpan.Parse(interval) };

        ValidateOptionsResult result = _validator.Validate(null, settings);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ContentApi:RemapPollInterval");
    }

    [Fact]
    public void ContentApiSettingsValidator_Validate_NegativeInitialDelay_Fails()
    {
        ContentApiSettings settings = new() { RemapInitialDelay = TimeSpan.FromSeconds(-1) };

        ValidateOptionsResult result = _validator.Validate(null, settings);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ContentApi:RemapInitialDelay");
    }

    [Fact]
    public void ContentApiSettingsValidator_Validate_ZeroInitialDelay_Succeeds()
    {
        ContentApiSettings settings = new() { RemapInitialDelay = TimeSpan.Zero };

        ValidateOptionsResult result = _validator.Validate(null, settings);

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("content-api:8080")]
    [InlineData("ftp://content-api")]
    public void ContentApiSettingsValidator_Validate_InvalidBaseUrl_Fails(string baseUrl)
    {
        ContentApiSettings settings = new() { BaseUrl = baseUrl };

        ValidateOptionsResult result = _validator.Validate(null, settings);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ContentApi:BaseUrl");
    }
}
