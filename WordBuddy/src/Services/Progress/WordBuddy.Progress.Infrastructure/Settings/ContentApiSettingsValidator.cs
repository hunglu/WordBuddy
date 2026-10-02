using Microsoft.Extensions.Options;

namespace WordBuddy.Progress.Infrastructure.Settings;

/// <summary>Validates <see cref="ContentApiSettings"/> at startup (<c>ValidateOnStart</c>), so a
/// misconfigured poll interval or initial delay stops the host with a clear message instead of
/// throwing inside <c>VocabularyIdRemapSyncService</c>.</summary>
public sealed class ContentApiSettingsValidator : IValidateOptions<ContentApiSettings>
{
    /// <summary>Largest period <see cref="PeriodicTimer"/> and <see cref="Task.Delay(TimeSpan)"/> accept.</summary>
    private static readonly TimeSpan MaxTimerPeriod = TimeSpan.FromMilliseconds(uint.MaxValue - 1);

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, ContentApiSettings options)
    {
        List<string> failures = [];

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out Uri? baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            failures.Add($"{ContentApiSettings.SectionName}:BaseUrl must be an absolute http(s) URL.");
        }

        if (options.RemapPollInterval <= TimeSpan.Zero || options.RemapPollInterval > MaxTimerPeriod)
        {
            failures.Add(
                $"{ContentApiSettings.SectionName}:RemapPollInterval must be greater than 00:00:00 and at most {MaxTimerPeriod} (was {options.RemapPollInterval}).");
        }

        if (options.RemapInitialDelay < TimeSpan.Zero || options.RemapInitialDelay > MaxTimerPeriod)
        {
            failures.Add(
                $"{ContentApiSettings.SectionName}:RemapInitialDelay must be between 00:00:00 and {MaxTimerPeriod} (was {options.RemapInitialDelay}).");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
